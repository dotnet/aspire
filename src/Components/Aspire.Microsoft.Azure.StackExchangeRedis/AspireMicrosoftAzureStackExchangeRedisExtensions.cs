// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Microsoft.Azure.StackExchangeRedis;
using Aspire.StackExchange.Redis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to an Azure Cache for Redis with StackExchange.Redis client
/// </summary>
public static class AspireMicrosoftAzureStackExchangeRedisExtensions
{
    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer"/> service for connecting Azure Cache for Redis with StackExchange.Redis client.
    /// Configures health check, logging and telemetry for the StackExchange.Redis client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing the <see cref="ConfigurationOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:StackExchange:Redis" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStackExchangeRedisSettings.ConnectionString"/> is not provided.</exception>
    public static void AddAzureRedisClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureStackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureStackExchangeRedisSettings? azureSettings = null;

        builder.AddRedisClient(connectionName, settings => azureSettings = ConfigureSettings(configureSettings, settings), options =>
        {
            Debug.Assert(azureSettings != null);

            var credential = azureSettings.Credential ?? new DefaultAzureCredential();
            // Configure Microsoft.Azure.StackExchangeRedis for Azure AD authentication
            // Note: Using GetAwaiter().GetResult() is acceptable here because this is configuration-time setup
            AzureCacheForRedis.ConfigureForAzureWithTokenCredentialAsync(options, credential).GetAwaiter().GetResult();

            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers <see cref="IConnectionMultiplexer"/> as a keyed service for given <paramref name="name"/> for connecting Azure Cache for Redis with StackExchange.Redis client.
    /// Configures health check, logging and telemetry for the StackExchange.Redis client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing the <see cref="ConfigurationOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:StackExchange:Redis:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureStackExchangeRedisSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedAzureRedisClient(this IHostApplicationBuilder builder, string name, Action<AzureStackExchangeRedisSettings>? configureSettings = null, Action<ConfigurationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        AzureStackExchangeRedisSettings? azureSettings = null;

        builder.AddKeyedRedisClient(name, settings => azureSettings = ConfigureSettings(configureSettings, settings), options =>
        {
            Debug.Assert(azureSettings != null);

            var credential = azureSettings.Credential ?? new DefaultAzureCredential();
            // Configure Microsoft.Azure.StackExchangeRedis for Azure AD authentication
            // Note: Using GetAwaiter().GetResult() is acceptable here because this is configuration-time setup
            AzureCacheForRedis.ConfigureForAzureWithTokenCredentialAsync(options, credential).GetAwaiter().GetResult();

            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="credential"></param>
    /// <returns></returns>
    public static AspireRedisClientBuilder WithAzureAuthentication(this AspireRedisClientBuilder builder, TokenCredential? credential = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var optionsName = builder.ServiceKey is null ? Options.Options.DefaultName : builder.ServiceKey;

        builder.HostBuilder.Services.Configure<ConfigurationOptions>(
            optionsName,
            configurationOptions =>
            {
                credential ??= new DefaultAzureCredential();

                // Configure Microsoft.Azure.StackExchangeRedis for Azure AD authentication
                // Note: Using GetAwaiter().GetResult() is acceptable here because this is configuration-time setup
                AzureCacheForRedis.ConfigureForAzureWithTokenCredentialAsync(configurationOptions, credential).GetAwaiter().GetResult();
            });

        return builder;
    }

    private static AzureStackExchangeRedisSettings ConfigureSettings(Action<AzureStackExchangeRedisSettings>? userConfigureSettings, StackExchangeRedisSettings settings)
    {
        var azureSettings = new AzureStackExchangeRedisSettings();

        // Copy the values updated by Redis integration.
        CopySettings(settings, azureSettings);

        // Invoke the Aspire configuration.
        userConfigureSettings?.Invoke(azureSettings);

        // Copy to the Redis integration settings as it needs to get any values set in userConfigureSettings.
        CopySettings(azureSettings, settings);

        return azureSettings;
    }

    private static void CopySettings(StackExchangeRedisSettings from, AzureStackExchangeRedisSettings to)
    {
        to.ConnectionString = from.ConnectionString;
        to.DisableHealthChecks = from.DisableHealthChecks;
        to.DisableTracing = from.DisableTracing;
    }

    private static void CopySettings(AzureStackExchangeRedisSettings from, StackExchangeRedisSettings to)
    {
        to.ConnectionString = from.ConnectionString;
        to.DisableHealthChecks = from.DisableHealthChecks;
        to.DisableTracing = from.DisableTracing;
    }
}
