// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class CallbackProxyTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public CallbackProxyTests()
    {
        _objectRegistry = new ObjectRegistry();
        _callbackInvoker = new TestCallbackInvoker();
        _operations = new RpcOperations(_objectRegistry, _callbackInvoker);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _operations.DisposeAsync();
    }

    [Fact]
    public void InvokeMethod_WithActionCallback_InvokesCallback()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        // Register the callback ID as an argument
        var args = JsonNode.Parse("{\"callback\": \"cb_action\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeAction", args);

        // The callback should have been invoked
        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_action", _callbackInvoker.Invocations[0].CallbackId);
    }

    [Fact]
    public void InvokeMethod_WithActionOfTCallback_PassesArgument()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_action_t\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeActionWithArg", args);

        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_action_t", _callbackInvoker.Invocations[0].CallbackId);
        Assert.Equal("test_value", (_callbackInvoker.Invocations[0].Args as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void InvokeMethod_WithFuncTaskCallback_InvokesAsyncCallback()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_func_task\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeFuncTask", args);

        // Callback is invoked synchronously by the proxy, recorded immediately
        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_func_task", _callbackInvoker.Invocations[0].CallbackId);
    }

    [Fact]
    public void InvokeMethod_WithFuncOfTTaskCallback_PassesArgument()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_func_t_task\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeFuncWithArgTask", args);

        // Callback is invoked synchronously by the proxy, recorded immediately
        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_func_t_task", _callbackInvoker.Invocations[0].CallbackId);
        // The argument should be marshalled since it's a complex object
        var invokedArgs = _callbackInvoker.Invocations[0].Args;
        Assert.NotNull(invokedArgs);
    }

    [Fact]
    public void InvokeMethod_WithFuncReturningValue_ReturnsCallbackResult()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        // Register a handler that returns a value
        _callbackInvoker.RegisterHandler("cb_func_result", 42);

        var args = JsonNode.Parse("{\"callback\": \"cb_func_result\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeFuncWithResult", args);

        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal(42, obj.LastResult);
    }

    [Fact]
    public void InvokeMethod_WithCancellationTokenCallback_PassesCancellationTokenRef()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_with_ct\"}") as JsonObject;

        // InvokeMethod awaits async results via UnwrapAsyncResult
        _operations.InvokeMethod(id, "TakeFuncWithCancellation", args);

        Assert.Single(_callbackInvoker.Invocations);
        // The callback args should contain both the CallbackArg and the CancellationToken as a ref
        // Note: Func<T1, T2, Task> parameters are named "arg1" and "arg2"
        var invokedArgs = _callbackInvoker.Invocations[0].Args;
        Assert.NotNull(invokedArgs);

        var jsonObj = invokedArgs as JsonObject;
        Assert.NotNull(jsonObj);

        // arg1 should be the CallbackArg (marshalled as object ref)
        Assert.True(jsonObj.ContainsKey("arg1"), "Should have arg1 (CallbackArg)");

        // arg2 should be the CancellationToken ref
        Assert.True(jsonObj.ContainsKey("arg2"), "CancellationToken should be passed as arg2");
        var ctNode = jsonObj["arg2"] as JsonObject;
        Assert.NotNull(ctNode);
        Assert.True(ctNode.ContainsKey("$cancellationToken"), "CancellationToken should be marshalled as {\"$cancellationToken\": \"ct_N\"}");
        var tokenId = ctNode["$cancellationToken"]?.GetValue<string>();
        Assert.NotNull(tokenId);
        Assert.StartsWith("ct_", tokenId);
    }

    [Fact]
    public void InvokeMethod_WithActionCancellationToken_PassesCancellationTokenRef()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_action_ct\"}") as JsonObject;

        _operations.InvokeMethod(id, "TakeActionWithCancellation", args);

        Assert.Single(_callbackInvoker.Invocations);
        // The invoked args should be an object with both the string value and the CancellationToken ref
        // Note: Action<T1, T2> delegate parameters are named "arg1" and "arg2"
        var invokedArgs = _callbackInvoker.Invocations[0].Args;
        Assert.NotNull(invokedArgs);

        var jsonObj = invokedArgs as JsonObject;
        Assert.NotNull(jsonObj);

        // Should have the string arg (named "arg1" for Action<T1, T2>)
        Assert.True(jsonObj.ContainsKey("arg1"), "Should have the string argument (arg1)");
        Assert.Equal("test_with_ct", jsonObj["arg1"]?.GetValue<string>());

        // Should have a cancellationToken property with $cancellationToken ref (named "arg2" for Action<T1, T2>)
        Assert.True(jsonObj.ContainsKey("arg2"), "CancellationToken should be passed as a ref (arg2)");
        var ctNode = jsonObj["arg2"] as JsonObject;
        Assert.NotNull(ctNode);
        Assert.True(ctNode.ContainsKey("$cancellationToken"), "CancellationToken should be marshalled as {\"$cancellationToken\": \"ct_N\"}");
    }

    [Fact]
    public void CancelToken_CancelsRegisteredToken()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"callback\": \"cb_cancel_test\"}") as JsonObject;

        // Invoke the method - this will create a linked CTS for the CancellationToken
        _operations.InvokeMethod(id, "TakeFuncWithObservableCancellation", args);

        Assert.Single(_callbackInvoker.Invocations);

        // For single-parameter delegates, the arg is marshalled directly (not in an object)
        // Func<CancellationToken, Task> -> single CancellationTokenRef
        var invokedArgs = _callbackInvoker.Invocations[0].Args as JsonObject;
        Assert.NotNull(invokedArgs);

        // The single arg is the CancellationToken ref directly
        var tokenId = invokedArgs["$cancellationToken"]?.GetValue<string>();
        Assert.NotNull(tokenId);
        Assert.StartsWith("ct_", tokenId);

        // Cancel via RPC - should return true (token exists)
        var cancelled = _operations.CancelToken(tokenId);
        Assert.True(cancelled, "CancelToken should return true for a valid token ID");

        // Cancelling again should also work (CTS can be cancelled multiple times)
        var cancelledAgain = _operations.CancelToken(tokenId);
        Assert.True(cancelledAgain);
    }

    [Fact]
    public void CancelToken_ReturnsFalseForUnknownToken()
    {
        var cancelled = _operations.CancelToken("ct_nonexistent");
        Assert.False(cancelled);
    }

    [Fact]
    public void CreateCancellationToken_ReturnsTokenRef()
    {
        var result = _operations.CreateCancellationToken();

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("$cancellationToken"));
        var tokenId = result["$cancellationToken"]?.GetValue<string>();
        Assert.NotNull(tokenId);
        Assert.StartsWith("ct_", tokenId);
    }

    [Fact]
    public void InvokeMethod_WithCancellationTokenParameter_ResolvesToken()
    {
        var obj = new ObjectWithCancellationToken();
        var id = _objectRegistry.Register(obj);

        // Create a token via RPC
        var tokenRef = _operations.CreateCancellationToken();
        var tokenId = tokenRef["$cancellationToken"]?.GetValue<string>();

        // Pass the token to a method
        var args = new JsonObject { ["cancellationToken"] = tokenRef };
        _operations.InvokeMethod(id, "DoWork", args);

        // The method should have received a valid CancellationToken
        Assert.NotNull(obj.ReceivedToken);
        Assert.False(obj.ReceivedToken.Value.IsCancellationRequested);

        // Cancel the token
        _operations.CancelToken(tokenId!);

        // The token should now be cancelled
        Assert.True(obj.ReceivedToken.Value.IsCancellationRequested);
    }

    [Fact]
    public void InvokeMethod_WithNullCancellationToken_UsesNone()
    {
        var obj = new ObjectWithCancellationToken();
        var id = _objectRegistry.Register(obj);

        // Pass null for cancellationToken
        var args = new JsonObject { ["cancellationToken"] = null };
        _operations.InvokeMethod(id, "DoWork", args);

        // Should receive CancellationToken.None
        Assert.NotNull(obj.ReceivedToken);
        Assert.Equal(CancellationToken.None, obj.ReceivedToken.Value);
    }

    #region Test Classes

    private sealed class ObjectWithCancellationToken
    {
        public CancellationToken? ReceivedToken { get; private set; }

        public void DoWork(CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
        }
    }

    private sealed class ObjectWithCallbacks
    {
        public int LastResult { get; private set; }
        public int CallCount { get; private set; }

        public void TakeAction(Action callback)
        {
            CallCount++;
            callback();
        }

        public void TakeActionWithArg(Action<string> callback)
        {
            CallCount++;
            callback("test_value");
        }

        public void TakeFuncTask(Func<Task> callback)
        {
            CallCount++;
            _ = callback();
        }

        public void TakeFuncWithArgTask(Func<CallbackArg, Task> callback)
        {
            CallCount++;
            _ = callback(new CallbackArg { Name = "async_arg", Value = 999 });
        }

        public void TakeFuncWithResult(Func<string, int> callback)
        {
            CallCount++;
            LastResult = callback("input");
        }

        public async Task TakeFuncWithCancellation(Func<CallbackArg, CancellationToken, Task> callback)
        {
            CallCount++;
            await callback(new CallbackArg { Name = "with_cancellation", Value = 42 }, CancellationToken.None);
        }

        public void TakeActionWithCancellation(Action<string, CancellationToken> callback)
        {
            CallCount++;
            callback("test_with_ct", CancellationToken.None);
        }

        public void TakeFuncWithObservableCancellation(Func<CancellationToken, Task> callback)
        {
            CallCount++;
            // Call the callback proxy with a CancellationToken - the proxy will marshal it
            _ = callback(CancellationToken.None);
        }
    }

    private sealed class CallbackArg
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    #endregion
}
