// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Azure CosmosDB extension
/// </summary>
public static class AspireAzureCosmosDBExtensions
{
    public const string DefaultConfigSectionName = "Aspire.Microsoft.Azure.Cosmos";

    /// <summary>
    /// Registers 'Scoped' <see cref="CosmosClient" /> factory for connecting Azure Cosmos DB.
    /// Configures health check, logging and telemetry for the <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configure">An optional method that can be used for customizing settings. It's invoked after the settings are read from the configuration.</param>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddAzureCosmosDB(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureDataCosmosSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AzureDataCosmosSettings settings = new();
        builder.Configuration.GetSection(DefaultConfigSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }
        configure?.Invoke(settings);

        builder.Services.AddScoped(_ => ConfigureDb());

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    eventCountersInstrumentationOptions.AddEventSources("Azure-Cosmos-Operation-Request-Diagnostics");
                });
            });
        }

        if (settings.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    "AzureCosmosDB",
                    sp => new AzureCosmosDBHealthCheck(settings),
                    failureStatus: default,
                    tags: default,
                    timeout: default));
        }

        CosmosClient ConfigureDb()
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{DefaultConfigSectionName}' configuration section.");
            }

            return new CosmosClient(settings.ConnectionString);
        }
    }
}
