// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ServiceDiscovery.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Microsoft.Extensions.ServiceDiscovery.Yarp;

internal sealed class ServiceDiscoveryForwarderHttpClientFactory(IServiceDiscoveryHttpMessageHandlerFactory handlerFactory)
    : ForwarderHttpClientFactory
{
    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        return handlerFactory.CreateHandler(handler);
    }
}
