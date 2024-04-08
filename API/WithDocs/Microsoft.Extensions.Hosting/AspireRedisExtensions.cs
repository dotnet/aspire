// Assembly 'Aspire.StackExchange.Redis'

using System;
using System.Collections.Generic;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Configuration;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Redis-related services in an <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireRedisExtensions
{
    /// <summary>
    /// Registers <see cref="T:StackExchange.Redis.IConnectionMultiplexer" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.StackExchange.Redis.StackExchangeRedisSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="T:StackExchange.Redis.ConfigurationOptions" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:StackExchange:Redis" section.</remarks>
    public static void AddRedisClient(this IHostApplicationBuilder builder, string connectionName, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);

    /// <summary>
    /// Registers <see cref="T:StackExchange.Redis.IConnectionMultiplexer" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.StackExchange.Redis.StackExchangeRedisSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional method that can be used for customizing the <see cref="T:StackExchange.Redis.ConfigurationOptions" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:StackExchange:Redis:{name}" section.</remarks>
    public static void AddKeyedRedisClient(this IHostApplicationBuilder builder, string name, Action<StackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null);
}
