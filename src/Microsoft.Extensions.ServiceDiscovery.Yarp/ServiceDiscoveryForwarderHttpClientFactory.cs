// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Microsoft.Extensions.ServiceDiscovery.Yarp;

internal sealed class ServiceDiscoveryForwarderHttpClientFactory(
    TimeProvider timeProvider,
    IServiceEndPointSelectorProvider selectorProvider,
    ServiceEndPointResolverFactory factory) : ForwarderHttpClientFactory
{
    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        var registry = new HttpServiceEndPointResolver(factory, selectorProvider, timeProvider);
        return new ResolvingHttpDelegatingHandler(registry, handler);
    }
}
