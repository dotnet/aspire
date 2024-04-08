// Assembly 'Aspire.StackExchange.Redis.OutputCaching'

using System;
using Aspire.StackExchange.Redis;
using Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for adding Redis output caching services to the <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireRedisOutputCacheExtensions
{
    /// <summary>
    /// Adds Redis output caching services in the services provided by the <paramref name="builder" />.
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
    public static void AddRedisOutputCache(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);

    /// <summary>
    /// Adds Redis output caching services for the given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
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
    public static void AddKeyedRedisOutputCache(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
