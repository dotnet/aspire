// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

internal sealed class ServiceDiscoveryHttpMessageHandlerMiddlewareFactory(
    TimeProvider timeProvider,
    IServiceEndPointSelectorFactory selectorProvider,
    ServiceEndPointWatcherFactory factory,
    IOptions<ServiceDiscoveryOptions> options) : IServiceDiscoveryDelegatingHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateServiceDiscoveryDelegatingHandler(HttpMessageHandler handler)
    {
        var registry = new HttpServiceEndPointResolver(factory, selectorProvider, timeProvider);
        return new ResolvingHttpDelegatingHandler(registry, options, handler);
    }
}
