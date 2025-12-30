// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
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
        var args = JsonDocument.Parse("{\"callback\": \"cb_action\"}").RootElement;

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

        var args = JsonDocument.Parse("{\"callback\": \"cb_action_t\"}").RootElement;

        _operations.InvokeMethod(id, "TakeActionWithArg", args);

        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_action_t", _callbackInvoker.Invocations[0].CallbackId);
        Assert.Equal("test_value", _callbackInvoker.Invocations[0].Args);
    }

    [Fact]
    public async Task InvokeMethod_WithFuncTaskCallback_InvokesAsyncCallback()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonDocument.Parse("{\"callback\": \"cb_func_task\"}").RootElement;

        _operations.InvokeMethod(id, "TakeFuncTask", args);

        // Give async callback time to complete
        await Task.Delay(50);

        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal("cb_func_task", _callbackInvoker.Invocations[0].CallbackId);
    }

    [Fact]
    public async Task InvokeMethod_WithFuncOfTTaskCallback_PassesArgument()
    {
        var obj = new ObjectWithCallbacks();
        var id = _objectRegistry.Register(obj);

        var args = JsonDocument.Parse("{\"callback\": \"cb_func_t_task\"}").RootElement;

        _operations.InvokeMethod(id, "TakeFuncWithArgTask", args);

        await Task.Delay(50);

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

        var args = JsonDocument.Parse("{\"callback\": \"cb_func_result\"}").RootElement;

        _operations.InvokeMethod(id, "TakeFuncWithResult", args);

        Assert.Single(_callbackInvoker.Invocations);
        Assert.Equal(42, obj.LastResult);
    }

    #region Test Classes

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
    }

    private sealed class CallbackArg
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    #endregion
}
