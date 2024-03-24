// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Qdrant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace Microsoft.Extensions.Hosting;

public static class AspireQdrantExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Qdrant";

    /// <summary>
    /// Registers <see cref="Qdrant.Client.QdrantClient" /> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Configures logging and telemetry for the <see cref="Qdrant.Client.QdrantClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="QdrantSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Qdrant" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddQdrantClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<QdrantSettings>? configureSettings = null)
    {
        AddQdrant(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="QdrantClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder"/>.
    /// Configures logging and telemetry for the <see cref="QdrantClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="QdrantSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Qdrant" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedQdrantClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<QdrantSettings>? configureSettings = null)
    {
        AddQdrant(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddQdrant(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<QdrantSettings>? configureSettings,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new QdrantSettings();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        if (builder.Configuration[$"Parameters:{connectionName}-ApiKey"] is string apiKey)
        {
            settings.ApiKey = apiKey;
        }

        configureSettings?.Invoke(settings);

        // TODO: Configure Tracing
        // TODO: Configure Health Checks
        // TODO: Configure Metrics

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(_ => ConfigureQdrant());
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, (sp, key) => ConfigureQdrant());
        }

        QdrantClient ConfigureQdrant()
        {
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                return new QdrantClient(new Uri(settings.ConnectionString), apiKey: settings.ApiKey);
            }
            else
            {
                throw new InvalidOperationException(
                        $"A QdrantClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                        $"{nameof(settings.ConnectionString)} must be provided " +
                        $"in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
