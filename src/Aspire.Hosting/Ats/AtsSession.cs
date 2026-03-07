// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Ats;

internal sealed class AtsSession : IAsyncDisposable
{
    private readonly HandleRegistry _handles;
    private readonly CancellationTokenRegistry _cancellationTokenRegistry;
    private readonly AtsCallbackProxyFactory _callbackProxyFactory;
    private readonly AtsMarshaller _marshaller;
    private readonly CapabilityDispatcher _dispatcher;

    public AtsSession(
        AtsContext context,
        Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> callbackInvoker,
        ILoggerFactory? loggerFactory = null)
    {
        _handles = new HandleRegistry();
        _cancellationTokenRegistry = new CancellationTokenRegistry();

        var callbackInvokerAdapter = new DelegateCallbackInvoker(callbackInvoker);
        AtsCallbackProxyFactory? callbackProxyFactory = null;
        var lazyCallbackProxyFactory = new Lazy<AtsCallbackProxyFactory>(() => callbackProxyFactory
            ?? throw new InvalidOperationException("Callback proxy factory was not initialized."));

        _marshaller = new AtsMarshaller(
            _handles,
            context,
            _cancellationTokenRegistry,
            lazyCallbackProxyFactory,
            new ReferenceExpressionFactory());

        callbackProxyFactory = new AtsCallbackProxyFactory(
            callbackInvokerAdapter,
            _handles,
            _cancellationTokenRegistry,
            _marshaller);

        _callbackProxyFactory = callbackProxyFactory;
        _dispatcher = new CapabilityDispatcher(
            _handles,
            _marshaller,
            context,
            loggerFactory?.CreateLogger<CapabilityDispatcher>());
    }

    public Task<JsonNode?> InvokeCapabilityAsync(string capabilityId, JsonObject? args)
    {
        return _dispatcher.InvokeAsync(capabilityId, args);
    }

    public bool CancelToken(string tokenId)
    {
        return _cancellationTokenRegistry.Cancel(tokenId);
    }

    public async ValueTask DisposeAsync()
    {
        _callbackProxyFactory.Dispose();
        _cancellationTokenRegistry.Dispose();
        await _handles.DisposeAsync().ConfigureAwait(false);
    }
}
