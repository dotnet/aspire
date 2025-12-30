// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Creates delegate proxies that invoke callbacks on the remote client.
/// Supports ANY delegate type via expression trees, with fast paths for common patterns.
/// </summary>
internal sealed class CallbackProxyFactory
{
    private readonly ICallbackInvoker _invoker;
    private readonly ObjectRegistry _objectRegistry;

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

        // Fast path for common patterns (avoid expression tree overhead)
        if (TryCreateKnownPattern(callbackId, delegateType, out var known))
        {
            return known;
        }

        // General fallback: expression trees handle any delegate signature
        return CreateDynamicProxy(callbackId, delegateType);
    }

    private bool TryCreateKnownPattern(string callbackId, Type delegateType, out Delegate? result)
    {
        // Action (no params, no return)
        if (delegateType == typeof(Action))
        {
            result = new Action(() => InvokeSyncVoid(callbackId, null));
            return true;
        }

        // Func<Task> (no params, async void)
        if (delegateType == typeof(Func<Task>))
        {
            result = new Func<Task>(() => InvokeAsyncVoid(callbackId, null));
            return true;
        }

        // Func<CancellationToken, Task> (cancellation, async void)
        if (delegateType == typeof(Func<CancellationToken, Task>))
        {
            result = new Func<CancellationToken, Task>(ct => InvokeAsyncVoid(callbackId, null, ct));
            return true;
        }

        // Handle generic Action<T>
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Action<>))
        {
            var argType = delegateType.GetGenericArguments()[0];
            var method = GetType().GetMethod(nameof(CreateActionProxy), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(argType);
            result = (Delegate)method.Invoke(this, [callbackId])!;
            return true;
        }

        // Handle Func<T, Task> (async with one arg)
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Func<,>))
        {
            var args = delegateType.GetGenericArguments();
            if (args[1] == typeof(Task))
            {
                var method = GetType().GetMethod(nameof(CreateAsyncActionProxy), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(args[0]);
                result = (Delegate)method.Invoke(this, [callbackId])!;
                return true;
            }

            // Handle Func<T, Task<TResult>> (async with return)
            if (args[1].IsGenericType && args[1].GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = args[1].GetGenericArguments()[0];
                var method = GetType().GetMethod(nameof(CreateAsyncFuncProxy), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(args[0], resultType);
                result = (Delegate)method.Invoke(this, [callbackId])!;
                return true;
            }

            // Handle Func<T, TResult> (sync with return)
            var funcMethod = GetType().GetMethod(nameof(CreateFuncProxy), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(args[0], args[1]);
            result = (Delegate)funcMethod.Invoke(this, [callbackId])!;
            return true;
        }

        // Handle Func<T1, T2, Task> (async with two args)
        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Func<,,>))
        {
            var args = delegateType.GetGenericArguments();
            if (args[2] == typeof(Task))
            {
                var method = GetType().GetMethod(nameof(CreateAsyncAction2Proxy), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(args[0], args[1]);
                result = (Delegate)method.Invoke(this, [callbackId])!;
                return true;
            }
        }

        result = null;
        return false;
    }

    private Delegate CreateDynamicProxy(string callbackId, Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke")!;
        var parameters = invokeMethod.GetParameters();
        var returnType = invokeMethod.ReturnType;

        // Build parameter expressions
        var paramExprs = parameters
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();

        // Build the body based on return type
        Expression body;

        if (returnType == typeof(void))
        {
            // Sync void: call InvokeSyncVoid
            body = BuildSyncVoidBody(callbackId, paramExprs);
        }
        else if (returnType == typeof(Task))
        {
            // Async void: call InvokeAsyncVoid
            body = BuildAsyncVoidBody(callbackId, paramExprs);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // Async with result: call InvokeAsyncResult<T>
            var resultType = returnType.GetGenericArguments()[0];
            body = BuildAsyncResultBody(callbackId, paramExprs, resultType);
        }
        else
        {
            // Sync with result: call InvokeSyncResult<T>
            body = BuildSyncResultBody(callbackId, paramExprs, returnType);
        }

        return Expression.Lambda(delegateType, body, paramExprs).Compile();
    }

    private Expression BuildSyncVoidBody(string callbackId, ParameterExpression[] paramExprs)
    {
        // this.InvokeSyncVoid(callbackId, BuildArgs(params))
        var argsExpr = BuildArgsExpression(paramExprs);
        var thisExpr = Expression.Constant(this);
        var callbackIdExpr = Expression.Constant(callbackId);

        return Expression.Call(
            thisExpr,
            typeof(CallbackProxyFactory).GetMethod(nameof(InvokeSyncVoid), BindingFlags.NonPublic | BindingFlags.Instance)!,
            callbackIdExpr,
            argsExpr);
    }

    private Expression BuildAsyncVoidBody(string callbackId, ParameterExpression[] paramExprs)
    {
        // this.InvokeAsyncVoid(callbackId, BuildArgs(params))
        var argsExpr = BuildArgsExpression(paramExprs);
        var thisExpr = Expression.Constant(this);
        var callbackIdExpr = Expression.Constant(callbackId);

        return Expression.Call(
            thisExpr,
            typeof(CallbackProxyFactory).GetMethod(nameof(InvokeAsyncVoid), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(object)])!,
            callbackIdExpr,
            argsExpr);
    }

    private Expression BuildAsyncResultBody(string callbackId, ParameterExpression[] paramExprs, Type resultType)
    {
        // this.InvokeAsyncResult<T>(callbackId, BuildArgs(params))
        var argsExpr = BuildArgsExpression(paramExprs);
        var thisExpr = Expression.Constant(this);
        var callbackIdExpr = Expression.Constant(callbackId);

        var method = typeof(CallbackProxyFactory)
            .GetMethod(nameof(InvokeAsyncResult), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(resultType);

        return Expression.Call(thisExpr, method, callbackIdExpr, argsExpr);
    }

    private Expression BuildSyncResultBody(string callbackId, ParameterExpression[] paramExprs, Type resultType)
    {
        // this.InvokeSyncResult<T>(callbackId, BuildArgs(params))
        var argsExpr = BuildArgsExpression(paramExprs);
        var thisExpr = Expression.Constant(this);
        var callbackIdExpr = Expression.Constant(callbackId);

        var method = typeof(CallbackProxyFactory)
            .GetMethod(nameof(InvokeSyncResult), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(resultType);

        return Expression.Call(thisExpr, method, callbackIdExpr, argsExpr);
    }

    private static Expression BuildArgsExpression(ParameterExpression[] paramExprs)
    {
        if (paramExprs.Length == 0)
        {
            return Expression.Constant(null, typeof(object));
        }

        if (paramExprs.Length == 1)
        {
            // Single arg - just box it
            return Expression.Convert(paramExprs[0], typeof(object));
        }

        // Multiple args - create anonymous object (dictionary)
        // We'll use a simple approach: create a dictionary
        var dictType = typeof(Dictionary<string, object>);
        var dictCtor = dictType.GetConstructor(Type.EmptyTypes)!;
        var addMethod = dictType.GetMethod("Add")!;

        var dictVar = Expression.Variable(dictType, "args");
        var expressions = new List<Expression>
        {
            Expression.Assign(dictVar, Expression.New(dictCtor))
        };

        foreach (var param in paramExprs)
        {
            expressions.Add(Expression.Call(
                dictVar,
                addMethod,
                Expression.Constant(param.Name!),
                Expression.Convert(param, typeof(object))));
        }

        expressions.Add(dictVar);

        return Expression.Block(dictType, [dictVar], expressions);
    }

    #region Helper Methods for Known Patterns

    private Action<T> CreateActionProxy<T>(string callbackId)
    {
        // Use Task.Run to avoid blocking the RPC dispatcher thread
        return arg => InvokeSyncVoid(callbackId, MarshalArg(arg));
    }

    private Func<T, Task> CreateAsyncActionProxy<T>(string callbackId)
    {
        return arg => InvokeAsyncVoid(callbackId, MarshalArg(arg));
    }

    private Func<T, TResult> CreateFuncProxy<T, TResult>(string callbackId)
    {
        return arg => InvokeSyncResult<TResult>(callbackId, MarshalArg(arg));
    }

    private Func<T, Task<TResult>> CreateAsyncFuncProxy<T, TResult>(string callbackId)
    {
        return arg => InvokeAsyncResult<TResult>(callbackId, MarshalArg(arg));
    }

    private Func<T1, T2, Task> CreateAsyncAction2Proxy<T1, T2>(string callbackId)
    {
        return (arg1, arg2) => InvokeAsyncVoid(callbackId, new { arg1 = MarshalArg(arg1), arg2 = MarshalArg(arg2) });
    }

    #endregion

    #region Invocation Methods

    private void InvokeSyncVoid(string callbackId, object? args)
    {
        // Use Task.Run to avoid blocking the RPC dispatcher thread.
        // Without this, if the callback calls back to .NET, we'd deadlock
        // because the dispatcher can't process the response while blocked.
        Task.Run(() => _invoker.InvokeAsync(callbackId, args)).GetAwaiter().GetResult();
    }

    private Task InvokeAsyncVoid(string callbackId, object? args, CancellationToken ct = default)
    {
        return _invoker.InvokeAsync(callbackId, args, ct);
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

    #region Argument Marshalling

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

    #endregion
}
