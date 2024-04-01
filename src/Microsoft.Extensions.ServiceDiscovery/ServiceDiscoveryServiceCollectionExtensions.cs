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
    {
        return services.AddServiceDiscoveryCore()
            .AddConfigurationServiceEndPointResolver()
            .AddPassThroughServiceEndPointResolver();
    }

    /// <summary>
    /// Adds the core service discovery services and configures defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The delegate used to configure service discovery options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, Action<ServiceDiscoveryOptions>? configureOptions)
    {
        return services.AddServiceDiscoveryCore(configureOptions: configureOptions)
            .AddConfigurationServiceEndPointResolver()
            .AddPassThroughServiceEndPointResolver();
    }

    /// <summary>
    /// Adds the core service discovery services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscoveryCore(this IServiceCollection services) => services.AddServiceDiscoveryCore(configureOptions: null);

    /// <summary>
    /// Adds the core service discovery services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The delegate used to configure service discovery options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddServiceDiscoveryCore(this IServiceCollection services, Action<ServiceDiscoveryOptions>? configureOptions)
    {
        services.AddOptions();
        services.AddLogging();
        services.TryAddTransient<IValidateOptions<ServiceDiscoveryOptions>, ServiceDiscoveryOptionsValidator>();
        services.TryAddSingleton(_ => TimeProvider.System);
        services.TryAddTransient<IServiceEndPointSelector, RoundRobinServiceEndPointSelector>();
        services.TryAddSingleton<ServiceEndPointWatcherFactory>();
        services.TryAddSingleton<IServiceDiscoveryHttpMessageHandlerFactory, ServiceDiscoveryHttpMessageHandlerFactory>();
        services.TryAddSingleton(sp => new ServiceEndPointResolver(sp.GetRequiredService<ServiceEndPointWatcherFactory>(), sp.GetRequiredService<TimeProvider>()));
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }

    /// <summary>
    /// Configures a service discovery endpoint resolver which uses <see cref="IConfiguration"/> to resolve endpoints.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConfigurationServiceEndPointResolver(this IServiceCollection services)
    {
        return services.AddConfigurationServiceEndPointResolver(configureOptions: null);
    }

    /// <summary>
    /// Configures a service discovery endpoint resolver which uses <see cref="IConfiguration"/> to resolve endpoints.
    /// </summary>
    /// <param name="configureOptions">The delegate used to configure the provider.</param>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConfigurationServiceEndPointResolver(this IServiceCollection services, Action<ConfigurationServiceEndPointResolverOptions>? configureOptions)
    {
        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndPointProviderFactory, ConfigurationServiceEndPointResolverProvider>();
        services.AddTransient<IValidateOptions<ConfigurationServiceEndPointResolverOptions>, ConfigurationServiceEndPointResolverOptionsValidator>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }

    /// <summary>
    /// Configures a service discovery endpoint resolver which passes through the input without performing resolution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPassThroughServiceEndPointResolver(this IServiceCollection services)
    {
        services.AddServiceDiscoveryCore();
        services.AddSingleton<IServiceEndPointProviderFactory, PassThroughServiceEndPointResolverProvider>();
        return services;
    }
}
