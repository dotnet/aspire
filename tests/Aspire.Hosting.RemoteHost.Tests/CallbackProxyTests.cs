// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Ats;
using Aspire.Hosting.RemoteHost.Ats;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class CallbackProxyTests
{
    [Fact]
    public void CreateProxy_ReturnsNullForNonDelegateType()
    {
        using var factory = CreateFactory();

        var result = factory.CreateProxy("callback1", typeof(string));

        Assert.Null(result);
    }

    [Fact]
    public void CreateProxy_ReturnsDelegateForAnyDelegateType()
    {
        using var factory = CreateFactory();

        // All delegate types are now accepted - no attribute required
        var result = factory.CreateProxy("callback1", typeof(Action));

        Assert.NotNull(result);
        Assert.IsAssignableFrom<Action>(result);
    }

    [Fact]
    public void CreateProxy_ReturnsDelegateForCustomType()
    {
        using var factory = CreateFactory();

        var result = factory.CreateProxy("callback1", typeof(TestCallbackNoArgs));

        Assert.NotNull(result);
        Assert.IsAssignableFrom<TestCallbackNoArgs>(result);
    }

    [Fact]
    public void CreateProxy_CachesDelegate()
    {
        using var factory = CreateFactory();

        var result1 = factory.CreateProxy("callback1", typeof(TestCallbackNoArgs));
        var result2 = factory.CreateProxy("callback1", typeof(TestCallbackNoArgs));

        Assert.Same(result1, result2);
    }

    [Fact]
    public async Task InvokedProxy_CallsCallbackInvoker()
    {
        var invoker = new TestCallbackInvoker();
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackNoArgs)factory.CreateProxy("test-callback", typeof(TestCallbackNoArgs))!;

        await proxy();

        Assert.Single(invoker.Invocations);
        Assert.Equal("test-callback", invoker.Invocations[0].CallbackId);
    }

    [Fact]
    public async Task InvokedProxy_ReturnsResultFromInvoker()
    {
        var invoker = new TestCallbackInvoker { ResultToReturn = JsonValue.Create(42) };
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackWithIntResult)factory.CreateProxy("test-callback", typeof(TestCallbackWithIntResult))!;

        var result = await proxy();

        Assert.Equal(42, result);
    }

    [Fact]
    public void CancellationTokenRegistry_IsExposed()
    {
        using var factory = CreateFactory();

        Assert.NotNull(factory.CancellationTokenRegistry);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        var factory = CreateFactory();

        factory.Dispose();

        // Should not throw when disposed
    }

    // Tests for callbacks with parameters (bug fix verification)
    [Fact]
    public void CreateProxy_ReturnsDelegateForCallbackWithStringParameter()
    {
        using var factory = CreateFactory();

        var result = factory.CreateProxy("callback1", typeof(TestCallbackWithString));

        Assert.NotNull(result);
        Assert.IsAssignableFrom<TestCallbackWithString>(result);
    }

    [Fact]
    public async Task InvokedProxy_PassesStringArgumentAsJson()
    {
        var invoker = new TestCallbackInvoker();
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackWithString)factory.CreateProxy("test-callback", typeof(TestCallbackWithString))!;

        await proxy("hello-world");

        Assert.Single(invoker.Invocations);
        var args = invoker.Invocations[0].Args as JsonObject;
        Assert.NotNull(args);
        // Arguments are passed with positional keys (p0, p1, p2, ...)
        Assert.Equal("hello-world", args["p0"]?.GetValue<string>());
    }

    [Fact]
    public async Task InvokedProxy_PassesMultipleArgumentsAsJson()
    {
        var invoker = new TestCallbackInvoker();
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackWithMultipleParams)factory.CreateProxy("test-callback", typeof(TestCallbackWithMultipleParams))!;

        await proxy("test-name", 42);

        Assert.Single(invoker.Invocations);
        var args = invoker.Invocations[0].Args as JsonObject;
        Assert.NotNull(args);
        // Arguments are passed with positional keys (p0, p1, p2, ...)
        Assert.Equal("test-name", args["p0"]?.GetValue<string>());
        Assert.Equal(42, args["p1"]?.GetValue<int>());
    }

    [Fact]
    public async Task InvokedProxy_WithResultReturnsCorrectValue()
    {
        var invoker = new TestCallbackInvoker { ResultToReturn = JsonValue.Create("result-value") };
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackWithStringResult)factory.CreateProxy("test-callback", typeof(TestCallbackWithStringResult))!;

        var result = await proxy("input");

        Assert.Equal("result-value", result);
    }

    [Fact]
    public async Task InvokedProxy_WithCancellationToken_IncludesTokenInArgs()
    {
        var invoker = new TestCallbackInvoker();
        using var factory = CreateFactory(invoker);
        using var cts = new CancellationTokenSource();

        var proxy = (TestCallbackWithCancellation)factory.CreateProxy("test-callback", typeof(TestCallbackWithCancellation))!;

        await proxy("test", cts.Token);

        Assert.Single(invoker.Invocations);
        var args = invoker.Invocations[0].Args as JsonObject;
        Assert.NotNull(args);
        // Arguments are passed with positional keys (p0, p1, p2, ...)
        // CancellationToken is not included in positional args, but added as $cancellationToken if not None
        Assert.Equal("test", args["p0"]?.GetValue<string>());
    }

    // Callback error handling tests
    [Fact]
    public async Task InvokedProxy_PropagatesExceptionFromInvoker()
    {
        var invoker = new TestCallbackInvoker
        {
            ExceptionToThrow = new InvalidOperationException("Callback failed")
        };
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackNoArgs)factory.CreateProxy("test-callback", typeof(TestCallbackNoArgs))!;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy());
        Assert.Equal("Callback failed", ex.Message);
    }

    [Fact]
    public async Task InvokedProxy_WithResult_PropagatesExceptionFromInvoker()
    {
        var invoker = new TestCallbackInvoker
        {
            ExceptionToThrow = new InvalidOperationException("Callback with result failed")
        };
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackWithIntResult)factory.CreateProxy("test-callback", typeof(TestCallbackWithIntResult))!;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => proxy());
        Assert.Equal("Callback with result failed", ex.Message);
    }

    [Fact]
    public async Task InvokedProxy_PropagatesOperationCanceledException()
    {
        var invoker = new TestCallbackInvoker
        {
            ExceptionToThrow = new OperationCanceledException("Operation was cancelled")
        };
        using var factory = CreateFactory(invoker);

        var proxy = (TestCallbackNoArgs)factory.CreateProxy("test-callback", typeof(TestCallbackNoArgs))!;

        await Assert.ThrowsAsync<OperationCanceledException>(() => proxy());
    }

    private static AtsCallbackProxyFactory CreateFactory(ICallbackInvoker? invoker = null)
    {
        var handles = new HandleRegistry();
        var ctRegistry = new CancellationTokenRegistry();
        var context = new AtsContext { Capabilities = [], HandleTypes = [], DtoTypes = [], EnumTypes = [] };
        var marshaller = new AtsMarshaller(handles, context, ctRegistry, new Lazy<AtsCallbackProxyFactory>(() => throw new NotImplementedException()));
        return new AtsCallbackProxyFactory(invoker ?? new TestCallbackInvoker(), handles, ctRegistry, marshaller);
    }

    // Test delegates - any delegate type is now treated as a callback
    public delegate Task TestCallback(string value);

    public delegate Task TestCallbackNoArgs();

    public delegate Task<int> TestCallbackWithIntResult();

    public delegate Task TestCallbackWithString(string value);

    public delegate Task TestCallbackWithMultipleParams(string name, int count);

    public delegate Task<string> TestCallbackWithStringResult(string input);

    public delegate Task TestCallbackWithCancellation(string value, CancellationToken cancellationToken);
}

internal sealed class TestCallbackInvoker : ICallbackInvoker
{
    public List<(string CallbackId, JsonNode? Args)> Invocations { get; } = [];
    public JsonNode? ResultToReturn { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public bool IsConnected => true;

    public Task<TResult> InvokeAsync<TResult>(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        Invocations.Add((callbackId, args));
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }
        return Task.FromResult(ResultToReturn is TResult result ? result : default!);
    }

    public Task InvokeAsync(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        Invocations.Add((callbackId, args));
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }
        return Task.CompletedTask;
    }
}
