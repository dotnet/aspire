// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Data.Tables;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.Tables;
using HealthChecks.Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireTablesExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Data:Tables";

    /// <summary>
    /// Registers <see cref="TableServiceClient "/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataTablesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TableServiceClient, TableClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Data.Tables" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureDataTablesSettings.ConnectionString"/> nor <see cref="AzureDataTablesSettings.ServiceUri"/> is provided.</exception>
    public static void AddAzureTableService(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureDataTablesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null)
    {
        new TableServiceComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="TableServiceClient "/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureDataTablesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TableServiceClient, TableClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.Data.Tables:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureDataTablesSettings.ConnectionString"/> nor <see cref="AzureDataTablesSettings.ServiceUri"/> is provided.</exception>
    public static void AddKeyedAzureTableService(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureDataTablesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string configurationSectionName = TableServiceComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new TableServiceComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class TableServiceComponent : AzureComponent<AzureDataTablesSettings, TableServiceClient, TableClientOptions>
    {
        protected override IAzureClientBuilder<TableServiceClient, TableClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureDataTablesSettings settings)
        {
            var connectionString = settings.ConnectionString;

            return !string.IsNullOrEmpty(connectionString) ?
                azureFactoryBuilder.AddTableServiceClient(connectionString) :
                azureFactoryBuilder.AddTableServiceClient(settings.ServiceUri);
        }

        protected override IHealthCheck CreateHealthCheck(TableServiceClient client, AzureDataTablesSettings settings)
            => new AzureTableServiceHealthCheck(client, new AzureTableServiceHealthCheckOptions());

        protected override bool GetHealthCheckEnabled(AzureDataTablesSettings settings)
            => settings.HealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureDataTablesSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureDataTablesSettings settings)
            => settings.Tracing;

        protected override void Validate(AzureDataTablesSettings settings, string connectionName, string configurationSectionName)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString) && settings.ServiceUri is null)
            {
                throw new InvalidOperationException($"A TableServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
            }
        }
    }
}
