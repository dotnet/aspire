// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        builder.AddRedisDistributedCacheCore((RedisCacheOptions options, IServiceProvider sp) =>
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

        builder.AddRedisDistributedCacheCore((RedisCacheOptions options, IServiceProvider sp) =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredKeyedService<IConnectionMultiplexer>(name));
        });
    }

    /// <summary>
    /// Configures the Redis client to also provide distributed caching services through <see cref="IDistributedCache"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AspireRedisClientBuilder"/> to configure.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="RedisCacheOptions"/>.</param>
    /// <returns>The <see cref="AspireRedisClientBuilder"/> for method chaining.</returns>
    /// <example>
    /// The following example creates an IDistributedCache service using the Redis client connection named "redis".
    /// <code lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.AddRedisClientBuilder("redis")
    ///        .WithDistributedCache();
    /// </code>
    /// The created IDistributedCache service can then be resolved from an IServiceProvider:
    /// <code lang="csharp">
    /// IServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
    ///
    /// var cache = serviceProvider.GetRequiredService&lt;IDistributedCache&gt;();
    /// </code>
    /// </example>
    public static AspireRedisClientBuilder WithDistributedCache(this AspireRedisClientBuilder builder, Action<RedisCacheOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HostBuilder.AddRedisDistributedCacheCore((RedisCacheOptions options, IServiceProvider sp) =>
        {
            var key = builder.ServiceKey;
            if (key is null)
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredService<IConnectionMultiplexer>());
            }
            else
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredKeyedService<IConnectionMultiplexer>(key));
            }

            configureOptions?.Invoke(options);
        });

        return builder;
    }

    private static void AddRedisDistributedCacheCore(this IHostApplicationBuilder builder, Action<RedisCacheOptions, IServiceProvider> configureRedisOptions)
    {
        builder.Services.AddStackExchangeRedisCache(static _ => { });

        builder.Services.AddOptions<RedisCacheOptions>() // note that RedisCacheOptions doesn't support named options
            .Configure(configureRedisOptions);
    }

    /// <summary>
    /// Configures the Redis client to provide a keyed distributed caching service through <see cref="IDistributedCache"/> using <paramref name="name"/> as the key.
    /// </summary>
    /// <param name="builder">The <see cref="AspireRedisClientBuilder"/> to configure.</param>
    /// <param name="name">The name which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="RedisCacheOptions"/>.</param>
    /// <returns>The <see cref="AspireRedisClientBuilder"/> for method chaining.</returns>
    /// <example>
    /// The following example creates keyed IDistributedCache service using the "myCache" key for a Redis client connection named "redis".
    /// <code lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.AddRedisClientBuilder("redis")
    ///        .WithKeyedDistributedCache("myCache", options => options.InstanceName = "myCache");
    /// </code>
    /// The created IDistributedCache service can then be resolved using the "myCache" key:
    /// <code lang="csharp">
    /// IServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
    ///
    /// var cache = serviceProvider.GetRequiredKeyedService&lt;IDistributedCache&gt;("myCache");
    /// </code>
    /// </example>
    public static AspireRedisClientBuilder WithKeyedDistributedCache(this AspireRedisClientBuilder builder, string name, Action<RedisCacheOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (configureOptions is not null)
        {
            // note that AddStackExchangeRedisCache doesn't support named RedisCacheOptions options, so we need to register the named options ourselves
            builder.HostBuilder.Services
                .AddOptions<RedisCacheOptions>(name)
                .Configure(configureOptions);
        }

        builder.HostBuilder.Services.AddKeyedSingleton<IDistributedCache>(name, (sp, key) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<RedisCacheOptions>>().Get((string?)key);

            options.ConnectionMultiplexerFactory = () => Task.FromResult(sp.GetRequiredKeyedService<IConnectionMultiplexer>(builder.ServiceKey));

            // AddStackExchangeRedisCache only supports unkeyed IDistributedCache, so we need to create the RedisCache instance ourselves.
            // As of .NET 10, the drawback to this approach is that it doesn't use the internal RedisCacheImpl class. Which means:
            // - The RedisCache won't log appropriately (but it only logs in one place - where it is unable to add library name suffix.
            // - If HybridCache is being used, it won't add the 'HC' library name suffix to the connection.
            return new RedisCache(options);
        });

        return builder;
    }
}
