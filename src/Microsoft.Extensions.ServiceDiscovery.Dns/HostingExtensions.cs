// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Dns;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/> to add service discovery.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds DNS SRV service discovery to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The DNS SRV service discovery configuration options.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// DNS SRV queries are able to provide port numbers for endpoints and can support multiple named endpoints per service.
    /// However, not all environment support DNS SRV queries, and in some environments, additional configuration may be required.
    /// </remarks>
    public static IServiceCollection AddDnsSrvServiceEndPointResolver(this IServiceCollection services, Action<DnsSrvServiceEndPointResolverOptions>? configureOptions = null)
    {
        services.AddServiceDiscoveryCore();
        services.TryAddSingleton<IDnsQuery, LookupClient>();
        services.AddSingleton<IServiceEndPointResolverProvider, DnsSrvServiceEndPointResolverProvider>();
        var options = services.AddOptions<DnsSrvServiceEndPointResolverOptions>();
        options.Configure(o => configureOptions?.Invoke(o));
        return services;
    }

    /// <summary>
    /// Adds DNS service discovery to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The DNS SRV service discovery configuration options.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// DNS A/AAAA queries are widely available but are not able to provide port numbers for endpoints and cannot support multiple named endpoints per service.
    /// </remarks>
    public static IServiceCollection AddDnsServiceEndPointResolver(this IServiceCollection services, Action<DnsServiceEndPointResolverOptions>? configureOptions = null)
    {
        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndPointResolverProvider, DnsServiceEndPointResolverProvider>();
        var options = services.AddOptions<DnsServiceEndPointResolverOptions>();
        options.Configure(o => configureOptions?.Invoke(o));
        return services;
    }
}
