// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Microsoft.Azure.StackExchangeRedis;
using Aspire.StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to an Azure Cache for Redis with StackExchange.Redis client
/// </summary>
public static class AspireMicrosoftAzureStackExchangeRedisExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:Azure:StackExchange:Redis";

    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer"/> service for connecting Azure Cache for Redis with StackExchange.Redis client.
    /// Configures health check, logging and telemetry for the StackExchange.Redis client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing the <see cref="ConfigurationOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:StackExchange:Redis" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStackExchangeRedisSettings.ConnectionString"/> is not provided.</exception>
    public static void AddAzureRedisClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureStackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AddAzureRedisClient(builder, configureSettings, configureOptions, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer"/> as a keyed service for given <paramref name="name"/> for connecting Azure Cache for Redis with StackExchange.Redis client.
    /// Configures health check, logging and telemetry for the StackExchange.Redis client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing the <see cref="ConfigurationOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:StackExchange:Redis:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStackExchangeRedisSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedAzureRedisClient(this IHostApplicationBuilder builder, string name, Action<AzureStackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddAzureRedisClient(builder, configureSettings, configureOptions, connectionName: name, serviceKey: name);
    }

    private static void AddAzureRedisClient(
        IHostApplicationBuilder builder,
        Action<AzureStackExchangeRedisSettings>? configureSettings,
        Action<ConfigurationOptions>? configureOptions,
        string connectionName,
        object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var configSection = builder.Configuration.GetSection(DefaultConfigSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);

        AzureStackExchangeRedisSettings settings = new();
        
        // Manually bind configuration properties, excluding Credential which can't be bound from config
        if (configSection["ConnectionString"] is string globalConnectionString)
        {
            settings.ConnectionString = globalConnectionString;
        }
        if (configSection["DisableHealthChecks"] is string globalDisableHealthChecks && bool.TryParse(globalDisableHealthChecks, out var globalHealthChecks))
        {
            settings.DisableHealthChecks = globalHealthChecks;
        }
        if (configSection["DisableTracing"] is string globalDisableTracing && bool.TryParse(globalDisableTracing, out var globalTracing))
        {
            settings.DisableTracing = globalTracing;
        }

        // Named configuration overrides global configuration
        if (namedConfigSection["ConnectionString"] is string namedConnectionString)
        {
            settings.ConnectionString = namedConnectionString;
        }
        if (namedConfigSection["DisableHealthChecks"] is string namedDisableHealthChecks && bool.TryParse(namedDisableHealthChecks, out var namedHealthChecks))
        {
            settings.DisableHealthChecks = namedHealthChecks;
        }
        if (namedConfigSection["DisableTracing"] is string namedDisableTracing && bool.TryParse(namedDisableTracing, out var namedTracing))
        {
            settings.DisableTracing = namedTracing;
        }

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        // Create StackExchangeRedisSettings from AzureStackExchangeRedisSettings
        Action<StackExchangeRedisSettings> configureRedisSettings = redisConfig =>
        {
            // Copy Azure settings to Redis settings
            redisConfig.ConnectionString = settings.ConnectionString;
            redisConfig.DisableHealthChecks = settings.DisableHealthChecks;
            redisConfig.DisableTracing = settings.DisableTracing;
        };

        Action<ConfigurationOptions> configureRedisOptions = options =>
        {
            // Configure Azure authentication if credential is provided
            if (settings.Credential != null)
            {
                // Configure Microsoft.Azure.StackExchangeRedis for Azure AD authentication
                // Note: Using GetAwaiter().GetResult() is acceptable here because this is configuration-time setup
                AzureCacheForRedis.ConfigureForAzureWithTokenCredentialAsync(options, settings.Credential).GetAwaiter().GetResult();
            }

            configureOptions?.Invoke(options);
        };

        if (serviceKey is null)
        {
            builder.AddRedisClient(connectionName, configureRedisSettings, configureRedisOptions);
        }
        else
        {
            builder.AddKeyedRedisClient(connectionName, configureRedisSettings, configureRedisOptions);
        }
    }
}