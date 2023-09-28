// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery.Yarp;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/> used to register the ReverseProxy's components.
/// </summary>
public static class ReverseProxyServiceCollectionExtensions
{
    /// <summary>
    /// Provides a <see cref="IDestinationResolver"/> implementation which uses service discovery to resolve destinations.
    /// </summary>
    public static IReverseProxyBuilder AddServiceDiscoveryDestinationResolver(this IReverseProxyBuilder builder)
    {
        builder.Services.AddServiceDiscoveryCore();
        builder.Services.AddSingleton<IDestinationResolver, ServiceDiscoveryDestinationResolver>();
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="IHttpForwarder"/> with service discovery support.
    /// </summary>
    public static IServiceCollection AddHttpForwarderWithServiceDiscovery(this IServiceCollection services)
    {
        return services.AddHttpForwarder().AddServiceDiscoveryForwarderFactory();
    }

    /// <summary>
    /// Provides a <see cref="IForwarderHttpClientFactory"/> implementation which uses service discovery to resolve service names.
    /// </summary>
    public static IServiceCollection AddServiceDiscoveryForwarderFactory(this IServiceCollection services)
    {
        services.AddServiceDiscoveryCore();
        services.AddSingleton<IForwarderHttpClientFactory, ServiceDiscoveryForwarderHttpClientFactory>();
        return services;
    }
}
