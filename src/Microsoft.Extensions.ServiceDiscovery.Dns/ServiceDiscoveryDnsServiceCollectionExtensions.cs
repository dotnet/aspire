// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.ServiceDiscovery.Dns;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/> to add service discovery.
/// </summary>
public static class ServiceDiscoveryDnsServiceCollectionExtensions
{
    /// <summary>
    /// Adds DNS SRV service discovery to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// DNS SRV queries are able to provide port numbers for endpoints and can support multiple named endpoints per service.
    /// However, not all environment support DNS SRV queries, and in some environments, additional configuration may be required.
    /// </remarks>
    public static IServiceCollection AddDnsSrvServiceEndpointProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDnsSrvServiceEndpointProvider(_ => { });
    }

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
    public static IServiceCollection AddDnsSrvServiceEndpointProvider(this IServiceCollection services, Action<DnsSrvServiceEndpointProviderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddServiceDiscoveryCore();

        if (!GetDnsClientFallbackFlag())
        {
            services.TryAddSingleton<IDnsResolver, DnsResolver>();
        }
        else
        {
            services.TryAddSingleton<IDnsResolver, FallbackDnsResolver>();
            services.TryAddSingleton<DnsClient.LookupClient>();
        }

        services.TryAddSingleton<IDnsResolver, DnsResolver>();
        services.AddSingleton<IServiceEndpointProviderFactory, DnsSrvServiceEndpointProviderFactory>();
        var options = services.AddOptions<DnsSrvServiceEndpointProviderOptions>();
        options.Configure(o => configureOptions?.Invoke(o));
        return services;

        static bool GetDnsClientFallbackFlag()
        {
            if (AppContext.TryGetSwitch("Microsoft.Extensions.ServiceDiscovery.Dns.UseDnsClientFallback", out var value))
            {
                return value;
            }

            var envVar = Environment.GetEnvironmentVariable("MICROSOFT_EXTENSIONS_SERVICE_DISCOVERY_DNS_USE_DNSCLIENT_FALLBACK");
            if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Adds DNS service discovery to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// DNS A/AAAA queries are widely available but are not able to provide port numbers for endpoints and cannot support multiple named endpoints per service.
    /// </remarks>
    public static IServiceCollection AddDnsServiceEndpointProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDnsServiceEndpointProvider(_ => { });
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
    public static IServiceCollection AddDnsServiceEndpointProvider(this IServiceCollection services, Action<DnsServiceEndpointProviderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndpointProviderFactory, DnsServiceEndpointProviderFactory>();
        var options = services.AddOptions<DnsServiceEndpointProviderOptions>();
        options.Configure(o => configureOptions?.Invoke(o));
        return services;
    }
}
