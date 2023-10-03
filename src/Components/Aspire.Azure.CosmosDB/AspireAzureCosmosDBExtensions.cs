// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace Aspire.Azure.CosmosDB;

/// <summary>
/// Azure CosmosDB extension
/// </summary>
public static class AspireAzureCosmosDBExtensions
{
    public const string DefaultConfigSectionName = "Aspire.Azure.CosmosDB";

    /// <summary>
    /// Registers 'Scoped' <see cref="CosmosClient" /> factory for connecting Azure CosmosDB.
    /// Configures health check, logging and telemetry for the <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configure">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="key">The sub-key of the configuration section. If not provided, the default value: <see cref="DefaultConfigSectionName"/></param>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddAzureCosmosDBConfig(this IHostApplicationBuilder builder, Action<AzureCosmosDBOptions>? configure = null,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        string configurationSectionName = key is null
            ? DefaultConfigSectionName
            : $"{DefaultConfigSectionName}:{key}";

        AzureCosmosDBOptions configurationOptions = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            configurationOptions.ConnectionString = builder.Configuration.GetConnectionString("Aspire.Azure.CosmosDB");
        }

        configure?.Invoke(configurationOptions);

        builder.Services.AddScoped<CosmosClient>(_ => ConfigureDb());

        if (configurationOptions.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (configurationOptions.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    eventCountersInstrumentationOptions.AddEventSources("Azure-Cosmos-Operation-Request-Diagnostics");
                });
            });
        }

        if (configurationOptions.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    "AzureCosmosDB",
                    sp => new AzureCosmosDBHealthCheck(configurationOptions),
                    failureStatus: default,
                    tags: default,
                    timeout: default));
        }

        CosmosClient ConfigureDb()
        {
            if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
            {
                throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{configurationSectionName}' configuration section.");
            }

            return new CosmosClient(configurationOptions.ConnectionString);
        }
    }
}
