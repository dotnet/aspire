// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

internal sealed class ServiceDiscoveryHttpMessageHandlerFactory(
    TimeProvider timeProvider,
    IServiceProvider serviceProvider,
    ServiceEndPointWatcherFactory factory,
    IOptions<ServiceDiscoveryOptions> options) : IServiceDiscoveryHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler(HttpMessageHandler handler)
    {
        var registry = new HttpServiceEndPointResolver(factory, serviceProvider, timeProvider);
        return new ResolvingHttpDelegatingHandler(registry, options, handler);
    }
}
