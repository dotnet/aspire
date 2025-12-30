// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Creates delegate proxies that invoke callbacks on the remote client.
/// Uses expression trees with caching for all delegate types.
/// </summary>
internal sealed class CallbackProxyFactory
{
    private readonly ICallbackInvoker _invoker;
    private readonly ObjectRegistry _objectRegistry;
    private readonly ConcurrentDictionary<(string CallbackId, Type DelegateType), Delegate> _cache = new();

    public CallbackProxyFactory(ICallbackInvoker invoker, ObjectRegistry objectRegistry)
    {
        _invoker = invoker;
        _objectRegistry = objectRegistry;
    }

    /// <summary>
    /// Creates a delegate proxy that invokes a callback on the remote client.
    /// </summary>
    /// <param name="callbackId">The callback ID registered on the client.</param>
    /// <param name="delegateType">The delegate type to create.</param>
    /// <returns>A delegate that invokes the remote callback, or null if the type is not a delegate.</returns>
    public Delegate? CreateProxy(string callbackId, Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
        {
            return null;
        }

        return _cache.GetOrAdd((callbackId, delegateType), key => BuildProxy(key.CallbackId, key.DelegateType));
    }

    /// <summary>
    /// Builds a delegate proxy using expression trees.
    /// </summary>
    /// <remarks>
    /// Generated code examples:
    /// <code>
    /// // Action (no params, void)
    /// () => InvokeSyncVoid("cb1", null)
    ///
    /// // Func&lt;Task&gt; (no params, async void)
    /// () => InvokeAsyncVoid("cb1", null)
    ///
    /// // Action&lt;T&gt; (single param, void)
    /// (T arg) => InvokeSyncVoid("cb1", MarshalArg(arg))
    ///
    /// // Func&lt;T, Task&gt; (single param, async void)
    /// (T arg) => InvokeAsyncVoid("cb1", MarshalArg(arg))
    ///
    /// // Func&lt;T, Task&lt;TResult&gt;&gt; (single param, async with result)
    /// (T arg) => InvokeAsyncResult&lt;TResult&gt;("cb1", MarshalArg(arg))
    ///
    /// // Action&lt;T1, T2&gt; (multiple params, void)
    /// (T1 arg1, T2 arg2) => {
    ///     var args = new Dictionary&lt;string, object?&gt;();
    ///     args.Add("arg1", MarshalArg(arg1));
    ///     args.Add("arg2", MarshalArg(arg2));
    ///     InvokeSyncVoid("cb1", args);
    /// }
    /// </code>
    /// </remarks>
    private Delegate BuildProxy(string callbackId, Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke")!;
        var parameters = invokeMethod.GetParameters();
        var returnType = invokeMethod.ReturnType;

        // Build parameter expressions
        var paramExprs = parameters
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();

        // Build marshalled args expression
        var argsExpr = BuildMarshalledArgsExpression(paramExprs);

        // Build the body based on return type
        Expression body = returnType switch
        {
            Type t when t == typeof(void) => BuildSyncVoidBody(callbackId, argsExpr),
            Type t when t == typeof(Task) => BuildAsyncVoidBody(callbackId, argsExpr),
            Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) =>
                BuildAsyncResultBody(callbackId, argsExpr, t.GetGenericArguments()[0]),
            _ => BuildSyncResultBody(callbackId, argsExpr, returnType)
        };

        return Expression.Lambda(delegateType, body, paramExprs).Compile();
    }

    private Expression BuildMarshalledArgsExpression(ParameterExpression[] paramExprs)
    {
        if (paramExprs.Length == 0)
        {
            return Expression.Constant(null, typeof(object));
        }

        // Get MarshalArg method
        var marshalMethod = typeof(CallbackProxyFactory)
            .GetMethod(nameof(MarshalArg), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var thisExpr = Expression.Constant(this);

        if (paramExprs.Length == 1)
        {
            // Single arg - marshal and return
            return Expression.Call(thisExpr, marshalMethod, Expression.Convert(paramExprs[0], typeof(object)));
        }

        // Multiple args - create dictionary with marshalled values
        var dictType = typeof(Dictionary<string, object?>);
        var dictCtor = dictType.GetConstructor(Type.EmptyTypes)!;
        var addMethod = dictType.GetMethod("Add")!;

        var dictVar = Expression.Variable(dictType, "args");
        var expressions = new List<Expression>
        {
            Expression.Assign(dictVar, Expression.New(dictCtor))
        };

        foreach (var param in paramExprs)
        {
            var marshalledValue = Expression.Call(thisExpr, marshalMethod, Expression.Convert(param, typeof(object)));
            expressions.Add(Expression.Call(dictVar, addMethod, Expression.Constant(param.Name!), marshalledValue));
        }

        expressions.Add(Expression.Convert(dictVar, typeof(object)));

        return Expression.Block(typeof(object), [dictVar], expressions);
    }

    private Expression BuildSyncVoidBody(string callbackId, Expression argsExpr)
    {
        // this.InvokeSyncVoid(callbackId, args)
        return Expression.Call(
            Expression.Constant(this),
            typeof(CallbackProxyFactory).GetMethod(nameof(InvokeSyncVoid), BindingFlags.NonPublic | BindingFlags.Instance)!,
            Expression.Constant(callbackId),
            argsExpr);
    }

    private Expression BuildAsyncVoidBody(string callbackId, Expression argsExpr)
    {
        // this.InvokeAsyncVoid(callbackId, args)
        return Expression.Call(
            Expression.Constant(this),
            typeof(CallbackProxyFactory).GetMethod(nameof(InvokeAsyncVoid), BindingFlags.NonPublic | BindingFlags.Instance)!,
            Expression.Constant(callbackId),
            argsExpr);
    }

    private Expression BuildAsyncResultBody(string callbackId, Expression argsExpr, Type resultType)
    {
        // this.InvokeAsyncResult<T>(callbackId, args)
        var method = typeof(CallbackProxyFactory)
            .GetMethod(nameof(InvokeAsyncResult), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(resultType);

        return Expression.Call(Expression.Constant(this), method, Expression.Constant(callbackId), argsExpr);
    }

    private Expression BuildSyncResultBody(string callbackId, Expression argsExpr, Type resultType)
    {
        // this.InvokeSyncResult<T>(callbackId, args)
        var method = typeof(CallbackProxyFactory)
            .GetMethod(nameof(InvokeSyncResult), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(resultType);

        return Expression.Call(Expression.Constant(this), method, Expression.Constant(callbackId), argsExpr);
    }

    #region Invocation Methods

    private void InvokeSyncVoid(string callbackId, object? args)
    {
        // Use Task.Run to avoid blocking the RPC dispatcher thread.
        // Without this, if the callback calls back to .NET, we'd deadlock
        // because the dispatcher can't process the response while blocked.
        Task.Run(() => _invoker.InvokeAsync(callbackId, args)).GetAwaiter().GetResult();
    }

    private Task InvokeAsyncVoid(string callbackId, object? args)
    {
        return _invoker.InvokeAsync(callbackId, args);
    }

    private TResult InvokeSyncResult<TResult>(string callbackId, object? args)
    {
        // Use Task.Run to avoid deadlock
        return Task.Run(() => _invoker.InvokeAsync<TResult>(callbackId, args)).GetAwaiter().GetResult();
    }

    private Task<TResult> InvokeAsyncResult<TResult>(string callbackId, object? args)
    {
        return _invoker.InvokeAsync<TResult>(callbackId, args);
    }

    #endregion

    private object? MarshalArg(object? arg)
    {
        if (arg == null)
        {
            return null;
        }

        if (ObjectRegistry.IsSimpleType(arg.GetType()))
        {
            return arg;
        }

        return _objectRegistry.Marshal(arg);
    }
}
