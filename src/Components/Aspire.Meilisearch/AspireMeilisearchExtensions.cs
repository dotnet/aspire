// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Meilisearch;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Meilisearch-related services in an <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireMeilisearchExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Meilisearch:Client";

    /// <summary>
    /// Registers <see cref="MeilisearchClient" /> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="MeilisearchClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Meilisearch:Client" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddMeilisearchClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<MeilisearchClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNullOrEmpty(connectionName);
        AddMeilisearchClient(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="Meilisearch" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="MeilisearchClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Meilisearch:Client" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedMeilisearchClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<MeilisearchClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        AddMeilisearchClient(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddMeilisearchClient(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<MeilisearchClientSettings>? configureSettings,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new MeilisearchClientSettings();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(ConfigureMeilisearchClient);
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, (sp, key) => ConfigureMeilisearchClient(sp));
        }

        if (!settings.DisableHealthChecks)
        {
            var healthCheckName = serviceKey is null ? "Meilisearch" : $"Meilisearch_{connectionName}";

            builder.TryAddHealthCheck(new HealthCheckRegistration(
                healthCheckName,
                sp => new MeilisearchHealthCheck(serviceKey is null ?
                    sp.GetRequiredService<MeilisearchClient>() :
                    sp.GetRequiredKeyedService<MeilisearchClient>(serviceKey)),
                failureStatus: null,
                tags: null,
                timeout: settings.HealthCheckTimeout > 0 ? TimeSpan.FromMilliseconds(settings.HealthCheckTimeout.Value) : null
                ));
        }

        MeilisearchClient ConfigureMeilisearchClient(IServiceProvider serviceProvider)
        {
            if (settings.Endpoint is not null)
            {
                return new MeilisearchClient(settings.Endpoint.ToString(), apiKey: settings.MasterKey);
            }
            else
            {
                throw new InvalidOperationException(
                        $"A MeilisearchClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                        $"{nameof(settings.Endpoint)} must be provided " +
                        $"in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
