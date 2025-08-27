// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for adding Redis distributed caching services to an <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireRedisDistributedCacheExtensions
{
    /// <summary>
    /// Adds Redis distributed caching services, <see cref="IDistributedCache"/>, in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="StackExchangeRedisSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="ConfigurationOptions"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:StackExchange:Redis" section.
    ///
    /// Also registers <see cref="IConnectionMultiplexer"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </remarks>
    public static void AddRedisDistributedCache(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<StackExchangeRedisSettings>? configureSettings = null,
        Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        builder.AddRedisClient(connectionName, configureSettings, configureOptions);

        builder.AddRedisDistributedCacheCore(null, (RedisCacheOptions options, IServiceProvider sp) =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredService<IConnectionMultiplexer>());
        });
    }

    /// <summary>
    /// Adds Redis distributed caching services, <see cref="IDistributedCache"/>, in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="StackExchangeRedisSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="ConfigurationOptions"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:StackExchange:Redis:{name}" section.
    ///
    /// Also registers <see cref="IConnectionMultiplexer"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </remarks>
    public static void AddKeyedRedisDistributedCache(
        this IHostApplicationBuilder builder,
        string name,
        Action<StackExchangeRedisSettings>? configureSettings = null,
        Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddKeyedRedisClient(name, configureSettings, configureOptions);

        builder.AddRedisDistributedCacheCore(name, (RedisCacheOptions options, IServiceProvider sp) =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredKeyedService<IConnectionMultiplexer>(name));
        });
    }

    /// <summary>
    /// Adds Redis distributed caching services, <see cref="IDistributedCache"/>, in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="serviceKey">The service key to use for the <see cref="IDistributedCache"/> registration and as the Redis key prefix.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="StackExchangeRedisSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="ConfigurationOptions"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:StackExchange:Redis:{name}" section.
    ///
    /// Also registers <see cref="IConnectionMultiplexer"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </remarks>
    public static void AddKeyedRedisDistributedCache(
        this IHostApplicationBuilder builder,
        string name,
        string serviceKey,
        Action<StackExchangeRedisSettings>? configureSettings = null,
        Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(serviceKey);

        builder.AddKeyedRedisClient(name, configureSettings, configureOptions);

        builder.AddRedisDistributedCacheCore(serviceKey, (RedisCacheOptions options, IServiceProvider sp) =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredKeyedService<IConnectionMultiplexer>(name));
            options.InstanceName = serviceKey; // Use service key as the Redis key prefix
        });
    }

    private static void AddRedisDistributedCacheCore(this IHostApplicationBuilder builder, string? serviceKey, Action<RedisCacheOptions, IServiceProvider> configureRedisOptions)
    {
        if (serviceKey is null)
        {
            // For non-keyed services, use the standard registration
            builder.Services.AddStackExchangeRedisCache(static _ => { });

            builder.Services.AddOptions<RedisCacheOptions>() // note that RedisCacheOptions doesn't support named options
                .Configure(configureRedisOptions);
        }
        else
        {
            // For keyed services, manually register IDistributedCache as a keyed service
            builder.Services.AddKeyedSingleton<IDistributedCache>(serviceKey, (serviceProvider, key) =>
            {
                var options = new RedisCacheOptions();
                configureRedisOptions(options, serviceProvider);
                return new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(options);
            });
        }
    }
}
