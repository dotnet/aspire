// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.ServiceDiscovery.Configuration;
using Microsoft.Extensions.ServiceDiscovery.Http;
using Microsoft.Extensions.ServiceDiscovery.Internal;
using Microsoft.Extensions.ServiceDiscovery.LoadBalancing;
using Microsoft.Extensions.ServiceDiscovery.PassThrough;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring service discovery.
/// </summary>
public static class ServiceDiscoveryServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core service discovery services and configures defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services)
        => AddServiceDiscoveryCore(services)
            .AddConfigurationServiceEndpointProvider()
            .AddPassThroughServiceEndpointProvider();

    /// <summary>
    /// Adds the core service discovery services and configures defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The delegate used to configure service discovery options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, Action<ServiceDiscoveryOptions> configureOptions)
        => AddServiceDiscoveryCore(services, configureOptions: configureOptions)
            .AddConfigurationServiceEndpointProvider()
            .AddPassThroughServiceEndpointProvider();

    /// <summary>
    /// Adds the core service discovery services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscoveryCore(this IServiceCollection services) => AddServiceDiscoveryCore(services, configureOptions: _ => { });

    /// <summary>
    /// Adds the core service discovery services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The delegate used to configure service discovery options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscoveryCore(this IServiceCollection services, Action<ServiceDiscoveryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions();
        services.AddLogging();
        services.TryAddTransient<IValidateOptions<ServiceDiscoveryOptions>, ServiceDiscoveryOptionsValidator>();
        services.TryAddSingleton(_ => TimeProvider.System);
        services.TryAddTransient<IServiceEndpointSelector, RoundRobinServiceEndpointSelector>();
        services.TryAddSingleton<ServiceEndpointWatcherFactory>();
        services.TryAddSingleton<IServiceDiscoveryHttpMessageHandlerFactory, ServiceDiscoveryHttpMessageHandlerFactory>();
        services.TryAddSingleton(sp => new ServiceEndpointResolver(sp.GetRequiredService<ServiceEndpointWatcherFactory>(), sp.GetRequiredService<TimeProvider>()));
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }

    /// <summary>
    /// Configures a service discovery endpoint provider which uses <see cref="IConfiguration"/> to resolve endpoints.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConfigurationServiceEndpointProvider(this IServiceCollection services)
        => AddConfigurationServiceEndpointProvider(services, configureOptions: _ => { });

    /// <summary>
    /// Configures a service discovery endpoint provider which uses <see cref="IConfiguration"/> to resolve endpoints.
    /// </summary>
    /// <param name="configureOptions">The delegate used to configure the provider.</param>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConfigurationServiceEndpointProvider(this IServiceCollection services, Action<ConfigurationServiceEndpointProviderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndpointProviderFactory, ConfigurationServiceEndpointProviderFactory>();
        services.AddTransient<IValidateOptions<ConfigurationServiceEndpointProviderOptions>, ConfigurationServiceEndpointProviderOptionsValidator>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }

    /// <summary>
    /// Configures a service discovery endpoint provider which passes through the input without performing resolution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPassThroughServiceEndpointProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndpointProviderFactory, PassThroughServiceEndpointProviderFactory>();
        return services;
    }
}
