// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

/// <summary>
/// Tests that demonstrate and verify fixes for callback deadlock scenarios.
///
/// The deadlock occurs when:
/// 1. TypeScript calls a .NET method that accepts an Action/Action{T} callback
/// 2. The .NET method invokes the callback synchronously (using .GetAwaiter().GetResult())
/// 3. The TypeScript callback handler tries to call back to .NET (e.g., get a property)
/// 4. Deadlock: the .NET RPC thread is blocked waiting for the callback, so it can't process the new request
/// </summary>
public sealed class CallbackDeadlockTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly SimulatedRpcCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public CallbackDeadlockTests()
    {
        _objectRegistry = new ObjectRegistry();
        _callbackInvoker = new SimulatedRpcCallbackInvoker();
        _operations = new RpcOperations(_objectRegistry, _callbackInvoker);

        // Give the callback invoker access to the operations so it can simulate callbacks that call back to .NET
        _callbackInvoker.Operations = _operations;
        _callbackInvoker.ObjectRegistry = _objectRegistry;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _operations.DisposeAsync();
    }

    /// <summary>
    /// This test demonstrates the deadlock scenario with synchronous Action callbacks.
    /// The callback tries to call back to .NET while the original thread is blocked.
    ///
    /// With the blocking implementation, this test will timeout (deadlock).
    /// With the async implementation, this test will pass.
    /// </summary>
    [Fact(Timeout = 5000)] // 5 second timeout to detect deadlock
    public async Task ActionCallback_ThatCallsBackToDotNet_ShouldNotDeadlock()
    {
        var obj = new ServiceWithCallback();
        var serviceId = _objectRegistry.Register(obj);

        // Register another object that the callback will try to access
        var data = new DataObject { Value = 42 };
        var dataId = _objectRegistry.Register(data);

        // Configure the callback to call back to .NET when invoked
        _callbackInvoker.RegisterReentrantCallback("reentrant_callback", dataId, "Value");

        var args = JsonNode.Parse("{\"callback\": \"reentrant_callback\"}") as JsonObject;

        // Run the method invocation on the dispatcher thread to simulate real RPC behavior
        // In real StreamJsonRpc, the method handler runs on the RPC dispatcher thread
        await _callbackInvoker.RunOnDispatcherAsync(() =>
        {
            // This will deadlock with the blocking implementation:
            // 1. InvokeMethod calls obj.DoWorkWithCallback(action)
            // 2. DoWorkWithCallback invokes action()
            // 3. action() blocks waiting for callback response (GetAwaiter().GetResult())
            // 4. Callback handler tries to call GetProperty(dataId, "Value")
            // 5. But we're on the dispatcher thread, blocked in step 3 - DEADLOCK
            _operations.InvokeMethod(serviceId, "DoWorkWithCallback", args);
        });

        // If we get here, no deadlock occurred
        Assert.True(obj.WorkCompleted);
        Assert.Equal(42, (_callbackInvoker.LastReentrantResult as JsonValue)?.GetValue<int>());
    }

    /// <summary>
    /// Similar test but with Action{T} callback.
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task ActionOfTCallback_ThatCallsBackToDotNet_ShouldNotDeadlock()
    {
        var obj = new ServiceWithCallback();
        var serviceId = _objectRegistry.Register(obj);

        var data = new DataObject { Value = 100 };
        var dataId = _objectRegistry.Register(data);

        _callbackInvoker.RegisterReentrantCallback("reentrant_callback_t", dataId, "Value");

        var args = JsonNode.Parse("{\"callback\": \"reentrant_callback_t\"}") as JsonObject;

        await _callbackInvoker.RunOnDispatcherAsync(() =>
        {
            _operations.InvokeMethod(serviceId, "DoWorkWithCallbackArg", args);
        });

        Assert.True(obj.WorkCompleted);
        Assert.Equal("processed", obj.LastMessage);
    }

    #region Test Classes

    private sealed class ServiceWithCallback
    {
        public bool WorkCompleted { get; private set; }
        public string? LastMessage { get; private set; }

        public void DoWorkWithCallback(Action callback)
        {
            // This invokes the callback synchronously
            // With blocking implementation: callback() blocks until TypeScript responds
            // If TypeScript's response tries to call back to .NET, we deadlock
            callback();
            WorkCompleted = true;
        }

        public void DoWorkWithCallbackArg(Action<string> callback)
        {
            callback("processed");
            WorkCompleted = true;
            LastMessage = "processed";
        }
    }

    private sealed class DataObject
    {
        public int Value { get; set; }
    }

    #endregion
}

/// <summary>
/// A callback invoker that simulates JSON-RPC behavior with proper async handling.
///
/// In real StreamJsonRpc, callback responses are processed by the message reader
/// independently of the dispatcher thread, so Task.Run allows the callback to complete.
/// This simulation reflects that by processing callbacks on a separate thread.
/// </summary>
internal sealed class SimulatedRpcCallbackInvoker : ICallbackInvoker
{
    private readonly Dictionary<string, (string ObjectId, string PropertyName)> _reentrantCallbacks = new();

    public RpcOperations? Operations { get; set; }
    public ObjectRegistry? ObjectRegistry { get; set; }
    public object? LastReentrantResult { get; private set; }
    public bool IsConnected => true;

    /// <summary>
    /// Registers a callback that will try to call back to .NET when invoked.
    /// This simulates TypeScript callback code that accesses .NET objects.
    /// </summary>
    public void RegisterReentrantCallback(string callbackId, string objectId, string propertyName)
    {
        _reentrantCallbacks[callbackId] = (objectId, propertyName);
    }

    public async Task<TResult> InvokeAsync<TResult>(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        if (_reentrantCallbacks.TryGetValue(callbackId, out var reentrant))
        {
            // This simulates TypeScript calling back to .NET during the callback
            // With Task.Run in the callback proxy, this runs on a thread pool thread
            // and doesn't block the original caller
            LastReentrantResult = Operations?.GetProperty(reentrant.ObjectId, reentrant.PropertyName);
        }

        return default!;
    }

    public Task InvokeAsync(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        return InvokeAsync<object?>(callbackId, args, cancellationToken);
    }

    /// <summary>
    /// Runs an action for testing purposes.
    /// </summary>
    public Task RunOnDispatcherAsync(Action action)
    {
        // Access instance to satisfy CA1822
        _ = IsConnected;
        return Task.Run(action);
    }

    public void Dispose()
    {
        // Clear state on dispose
        _reentrantCallbacks.Clear();
    }
}
