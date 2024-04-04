// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Qdrant.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qdrant.Client;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Qdrant-related services in an <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireQdrantExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Qdrant:Client";

    /// <summary>
    /// Registers <see cref="QdrantClient" /> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Configures logging for the <see cref="QdrantClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="QdrantClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Qdrant:Client" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddQdrantClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<QdrantClientSettings>? configureSettings = null)
    {
        AddQdrant(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="QdrantClient" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder"/>.
    /// Configures logging for the <see cref="QdrantClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="QdrantClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Qdrant:Client" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedQdrantClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<QdrantClientSettings>? configureSettings = null)
    {
        AddQdrant(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddQdrant(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<QdrantClientSettings>? configureSettings,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new QdrantClientSettings();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(ConfigureQdrant);
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, (sp, key) => ConfigureQdrant(sp));
        }

        QdrantClient ConfigureQdrant(IServiceProvider serviceProvider)
        {
            if (settings.Endpoint is not null)
            {
                return new QdrantClient(settings.Endpoint, apiKey: settings.Key, loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>());
            }
            else
            {
                throw new InvalidOperationException(
                        $"A QdrantClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                        $"{nameof(settings.Endpoint)} must be provided " +
                        $"in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
