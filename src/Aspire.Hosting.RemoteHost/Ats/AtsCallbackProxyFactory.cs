// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Creates delegate proxies for ATS callbacks that invoke the remote client.
/// Works with any delegate type.
/// </summary>
internal sealed class AtsCallbackProxyFactory : IDisposable
{
    private readonly ICallbackInvoker _invoker;
    private readonly HandleRegistry _handleRegistry;
    private readonly CancellationTokenRegistry _cancellationTokenRegistry;
    private readonly AtsMarshaller _marshaller;
    private readonly ConcurrentDictionary<(string CallbackId, Type DelegateType), Delegate> _cache = new();

    /// <summary>
    /// Creates a new AtsCallbackProxyFactory with the specified invoker and handle registry.
    /// </summary>
    /// <param name="invoker">The callback invoker for making remote calls.</param>
    /// <param name="handleRegistry">The handle registry for marshalling objects.</param>
    /// <param name="cancellationTokenRegistry">The cancellation token registry.</param>
    /// <param name="marshaller">The marshaller for converting objects to/from JSON.</param>
    public AtsCallbackProxyFactory(
        ICallbackInvoker invoker,
        HandleRegistry handleRegistry,
        CancellationTokenRegistry cancellationTokenRegistry,
        AtsMarshaller marshaller)
    {
        _invoker = invoker;
        _handleRegistry = handleRegistry;
        _cancellationTokenRegistry = cancellationTokenRegistry;
        _marshaller = marshaller;
    }

    /// <summary>
    /// Gets the cancellation token registry for external access (e.g., RPC cancel method).
    /// </summary>
    public CancellationTokenRegistry CancellationTokenRegistry => _cancellationTokenRegistry;

    /// <summary>
    /// Creates a delegate proxy that invokes a callback on the remote client.
    /// </summary>
    /// <param name="callbackId">The callback ID registered on the client.</param>
    /// <param name="delegateType">The delegate type to create.</param>
    /// <returns>A delegate that invokes the remote callback, or null if the type is not valid.</returns>
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
    private Delegate BuildProxy(string callbackId, Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke")!;
        var parameters = invokeMethod.GetParameters();
        var returnType = invokeMethod.ReturnType;

        // Create parameter expressions
        var paramExprs = parameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

        // Determine if async (returns Task or Task<T>)
        var isAsync = typeof(Task).IsAssignableFrom(returnType);
        var hasResult = returnType != typeof(void) && (!isAsync || returnType.IsGenericType);
        var resultType = isAsync && returnType.IsGenericType
            ? returnType.GetGenericArguments()[0]
            : (returnType != typeof(void) && !isAsync ? returnType : null);

        // Find CancellationToken parameter if any
        var ctParamIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(CancellationToken));
        Expression? ctExpr = ctParamIndex >= 0 ? paramExprs[ctParamIndex] : null;

        // Build the body expression
        Expression body;
        var argsExpr = parameters.Length == 0 || (parameters.Length == 1 && ctParamIndex == 0)
            ? null
            : BuildMarshalArgs(paramExprs, parameters);

        if (isAsync)
        {
            if (!hasResult)
            {
                body = BuildAsyncVoidCall(callbackId, argsExpr, ctExpr);
            }
            else
            {
                body = BuildAsyncResultCall(callbackId, resultType!, argsExpr, ctExpr);
            }
        }
        else
        {
            if (!hasResult)
            {
                body = BuildSyncVoidCall(callbackId, argsExpr, ctExpr);
            }
            else
            {
                body = BuildSyncResultCall(callbackId, resultType!, argsExpr, ctExpr);
            }
        }

        var lambda = Expression.Lambda(delegateType, body, paramExprs);
        return lambda.Compile();
    }

    private Expression BuildMarshalArgs(ParameterExpression[] paramExprs, ParameterInfo[] parameters)
    {
        // Build: new JsonObject { { "p0", MarshalArg(arg1) }, { "p1", MarshalArg(arg2) } }
        // Uses positional keys (p0, p1, p2, ...) instead of parameter names for predictable unpacking on TypeScript side
        var jsonObjectType = typeof(JsonObject);
        // JsonObject doesn't have a true parameterless constructor - it has JsonObject(JsonNodeOptions? options = null)
        // Expression.New can't handle optional parameters, so we need to call the constructor explicitly with null
        var jsonObjectCtor = jsonObjectType.GetConstructor([typeof(JsonNodeOptions?)])!;
        var newJsonObject = Expression.New(jsonObjectCtor, Expression.Constant(null, typeof(JsonNodeOptions?)));

        var addMethod = jsonObjectType.GetMethod("Add", [typeof(string), typeof(JsonNode)]);

        var expressions = new List<Expression>();
        var jsonObjVar = Expression.Variable(jsonObjectType, "args");
        expressions.Add(Expression.Assign(jsonObjVar, newJsonObject));

        var paramIndex = 0; // Track positional index (excludes CancellationToken)
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramExpr = paramExprs[i];

            // Skip CancellationToken for now (handled separately)
            if (param.ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            // Call MarshalArg to convert to JsonNode
            var marshalMethod = typeof(AtsCallbackProxyFactory).GetMethod(
                nameof(MarshalArg),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            var marshalCall = Expression.Call(
                Expression.Constant(this),
                marshalMethod,
                Expression.Convert(paramExpr, typeof(object)));

            // Use positional key (p0, p1, p2, ...) instead of param.Name
            var addCall = Expression.Call(jsonObjVar, addMethod!, Expression.Constant($"p{paramIndex}"), marshalCall);
            expressions.Add(addCall);
            paramIndex++;
        }

        expressions.Add(jsonObjVar);
        return Expression.Block(new[] { jsonObjVar }, expressions);
    }

    private JsonNode? MarshalArg(object? value)
    {
        return _marshaller.MarshalToJson(value);
    }

    private Expression BuildSyncVoidCall(string callbackId, Expression? argsExpr, Expression? ctExpr)
    {
        var invokeMethod = typeof(AtsCallbackProxyFactory).GetMethod(
            nameof(InvokeSyncVoid),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        return Expression.Call(
            Expression.Constant(this),
            invokeMethod,
            Expression.Constant(callbackId),
            argsExpr ?? Expression.Constant(null, typeof(JsonObject)),
            ctExpr ?? Expression.Constant(CancellationToken.None, typeof(CancellationToken)));
    }

    private Expression BuildSyncResultCall(string callbackId, Type resultType, Expression? argsExpr, Expression? ctExpr)
    {
        var invokeMethod = typeof(AtsCallbackProxyFactory).GetMethod(
            nameof(InvokeSyncResult),
            BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(resultType);

        return Expression.Call(
            Expression.Constant(this),
            invokeMethod,
            Expression.Constant(callbackId),
            argsExpr ?? Expression.Constant(null, typeof(JsonObject)),
            ctExpr ?? Expression.Constant(CancellationToken.None, typeof(CancellationToken)));
    }

    private Expression BuildAsyncVoidCall(string callbackId, Expression? argsExpr, Expression? ctExpr)
    {
        var invokeMethod = typeof(AtsCallbackProxyFactory).GetMethod(
            nameof(InvokeAsyncVoid),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        return Expression.Call(
            Expression.Constant(this),
            invokeMethod,
            Expression.Constant(callbackId),
            argsExpr ?? Expression.Constant(null, typeof(JsonObject)),
            ctExpr ?? Expression.Constant(CancellationToken.None, typeof(CancellationToken)));
    }

    private Expression BuildAsyncResultCall(string callbackId, Type resultType, Expression? argsExpr, Expression? ctExpr)
    {
        var invokeMethod = typeof(AtsCallbackProxyFactory).GetMethod(
            nameof(InvokeAsyncResult),
            BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(resultType);

        return Expression.Call(
            Expression.Constant(this),
            invokeMethod,
            Expression.Constant(callbackId),
            argsExpr ?? Expression.Constant(null, typeof(JsonObject)),
            ctExpr ?? Expression.Constant(CancellationToken.None, typeof(CancellationToken)));
    }

    private void InvokeSyncVoid(string callbackId, JsonObject? args, CancellationToken cancellationToken)
    {
        AddCancellationTokenToArgs(ref args, cancellationToken);
        _invoker.InvokeAsync<JsonNode?>(callbackId, args, cancellationToken).GetAwaiter().GetResult();
    }

    private T? InvokeSyncResult<T>(string callbackId, JsonObject? args, CancellationToken cancellationToken)
    {
        AddCancellationTokenToArgs(ref args, cancellationToken);
        var result = _invoker.InvokeAsync<JsonNode?>(callbackId, args, cancellationToken).GetAwaiter().GetResult();
        return UnmarshalResult<T>(result, callbackId);
    }

    private async Task InvokeAsyncVoid(string callbackId, JsonObject? args, CancellationToken cancellationToken)
    {
        AddCancellationTokenToArgs(ref args, cancellationToken);
        await _invoker.InvokeAsync<JsonNode?>(callbackId, args, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> InvokeAsyncResult<T>(string callbackId, JsonObject? args, CancellationToken cancellationToken)
    {
        AddCancellationTokenToArgs(ref args, cancellationToken);
        var result = await _invoker.InvokeAsync<JsonNode?>(callbackId, args, cancellationToken).ConfigureAwait(false);
        return UnmarshalResult<T>(result, callbackId);
    }

    private T? UnmarshalResult<T>(JsonNode? result, string callbackId)
    {
        if (result == null)
        {
            return default;
        }

        var context = new AtsMarshaller.UnmarshalContext
        {
            CapabilityId = $"callback:{callbackId}",
            ParameterName = "$result"
        };

        return (T?)_marshaller.UnmarshalFromJson(result, typeof(T), context);
    }

    private void AddCancellationTokenToArgs(ref JsonObject? args, CancellationToken cancellationToken)
    {
        if (cancellationToken != CancellationToken.None)
        {
            var (tokenId, _) = _cancellationTokenRegistry.CreateLinked(cancellationToken);
            args ??= new JsonObject();
            args["$cancellationToken"] = tokenId;
        }
    }

    public void Dispose()
    {
        // No-op - CancellationTokenRegistry is managed by DI
    }
}
