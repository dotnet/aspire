// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

internal sealed class ServiceDiscoveryHttpMessageHandlerMiddlewareFactory(
    TimeProvider timeProvider,
    IServiceProvider serviceProvider,
    ServiceEndPointWatcherFactory factory,
    IOptions<ServiceDiscoveryOptions> options) : IServiceDiscoveryDelegatingHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateServiceDiscoveryDelegatingHandler(HttpMessageHandler handler)
    {
        var registry = new HttpServiceEndPointResolver(factory, serviceProvider, timeProvider);
        return new ResolvingHttpDelegatingHandler(registry, options, handler);
    }
}
