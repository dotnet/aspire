// Assembly 'Aspire.StackExchange.Redis.DistributedCaching'

using System;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for adding Redis distributed caching services to an <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireRedisDistributedCacheExtensions
{
    /// <summary>
    /// Adds Redis distributed caching services, <see cref="T:Microsoft.Extensions.Caching.Distributed.IDistributedCache" />, in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.StackExchange.Redis.StackExchangeRedisSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="T:StackExchange.Redis.ConfigurationOptions" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:StackExchange:Redis" section.
    ///
    /// Also registers <see cref="T:StackExchange.Redis.IConnectionMultiplexer" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </remarks>
    public static void AddRedisDistributedCache(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);

    /// <summary>
    /// Adds Redis distributed caching services, <see cref="T:Microsoft.Extensions.Caching.Distributed.IDistributedCache" />, in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.StackExchange.Redis.StackExchangeRedisSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="T:StackExchange.Redis.ConfigurationOptions" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>
    /// Reads the configuration from "Aspire:StackExchange:Redis:{name}" section.
    ///
    /// Also registers <see cref="T:StackExchange.Redis.IConnectionMultiplexer" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </remarks>
    public static void AddKeyedRedisDistributedCache(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
