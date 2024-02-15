// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Cosmos;
using Aspire.Microsoft.Azure.Cosmos;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Azure CosmosDB extension
/// </summary>
public static class AspireAzureCosmosDBExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:Azure:Cosmos";

    /// <summary>
    /// Registers <see cref="CosmosClient" /> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Configures logging and telemetry for the <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureCosmosDBSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="CosmosClientOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:Cosmos" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddAzureCosmosDB(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureCosmosDBSettings>? configureSettings = null,
        Action<CosmosClientOptions>? configureClientOptions = null)
    {
        AddAzureCosmosDB(builder, DefaultConfigSectionName, configureSettings, configureClientOptions, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="CosmosClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder"/>.
    /// Configures logging and telemetry for the <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureCosmosDBSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="CosmosClientOptions"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:Cosmos:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedAzureCosmosDB(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureCosmosDBSettings>? configureSettings = null,
        Action<CosmosClientOptions>? configureClientOptions = null)
    {
        AddAzureCosmosDB(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, configureClientOptions, connectionName: name, serviceKey: name);
    }

    private static void AddAzureCosmosDB(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<AzureCosmosDBSettings>? configureSettings,
        Action<CosmosClientOptions>? configureClientOptions,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new AzureCosmosDBSettings();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                settings.AccountEndpoint = uri;
            }
            else
            {
                settings.ConnectionString = connectionString;
            }
        }

        configureSettings?.Invoke(settings);

        var clientOptions = new CosmosClientOptions();
        // Needs to be enabled for either logging or tracing to work.
        clientOptions.CosmosClientTelemetryOptions.DisableDistributedTracing = false;
        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (CosmosUtils.IsEmulatorConnectionString(settings.ConnectionString))
        {
            clientOptions.ConnectionMode = ConnectionMode.Gateway;
            clientOptions.LimitToEndpoint = true;
        }

        configureClientOptions?.Invoke(clientOptions);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton(_ => ConfigureDb());
        }
        else
        {
            builder.Services.AddKeyedSingleton(serviceKey, (sp, key) => ConfigureDb());
        }

        CosmosClient ConfigureDb()
        {
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                return new CosmosClient(settings.ConnectionString, clientOptions);
            }
            else if (settings.AccountEndpoint is not null)
            {
                var credential = settings.Credential ?? new DefaultAzureCredential();
                return new CosmosClient(settings.AccountEndpoint.OriginalString, credential, clientOptions);
            }
            else
            {
                throw new InvalidOperationException(
                        $"A CosmosClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                        $"{nameof(settings.ConnectionString)} or {nameof(settings.AccountEndpoint)} must be provided " +
                        $"in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
