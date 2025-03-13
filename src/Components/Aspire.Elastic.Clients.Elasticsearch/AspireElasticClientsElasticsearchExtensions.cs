// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting Elasticsearch with Elastic.Clients.Elasticsearch client.
/// </summary>
public static class AspireElasticClientsElasticsearchExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Elastic:Clients:Elasticsearch";
    private const string ActivityNameSource = "Elastic.Transport";

    /// <summary>
    /// Registers <see cref="ElasticsearchClient"/> instance for connecting to Elasticsearch with Elastic.Clients.Elasticsearch client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing ElasticsearchClientSettings.</param>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddElasticsearchClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<ElasticClientsElasticsearchSettings>? configureSettings = null,
        Action<ElasticsearchClientSettings>? configureClientSettings = null
        )
    {
        AddElasticsearchClient(builder, configureSettings, configureClientSettings, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="ElasticsearchClient"/> instance for connecting to Elasticsearch with Elastic.Clients.Elasticsearch client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing ElasticsearchClientSettings.</param>
    /// <exception cref="InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedElasticsearchClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<ElasticClientsElasticsearchSettings>? configureSettings = null,
        Action<ElasticsearchClientSettings>? configureClientSettings = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddElasticsearchClient(
            builder,
            configureSettings,
            configureClientSettings,
            connectionName: name,
            serviceKey: name);
    }

    private static void AddElasticsearchClient(
        this IHostApplicationBuilder builder,
        Action<ElasticClientsElasticsearchSettings>? configureSettings,
        Action<ElasticsearchClientSettings>? configureClientSettings,
        string connectionName,
        object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var configSection = builder.Configuration.GetSection(DefaultConfigSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);

        ElasticClientsElasticsearchSettings settings = new();
        configSection.Bind(settings);
        namedConfigSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseConnectionString(connectionString);
        }

        configureSettings?.Invoke(settings);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ElasticsearchClient>(CreateElasticsearchClient);
        }
        else
        {
            builder.Services.AddKeyedSingleton<ElasticsearchClient>(serviceKey, (sp, key) => CreateElasticsearchClient(sp));
        }

        if (!settings.DisableTracing)
        {
            builder.Services
                .AddOpenTelemetry()
                .WithTracing(tracer => tracer.AddSource(ActivityNameSource));
        }

        if (!settings.DisableHealthChecks)
        {
            var healthCheckName = serviceKey is null ? "Elastic.Clients.Elasticsearch" : $"Elastic.Clients.Elasticsearch_{connectionName}";

            builder.TryAddHealthCheck(new HealthCheckRegistration(
                healthCheckName,
                sp => new ElasticsearchHealthCheck(serviceKey is null ?
                    sp.GetRequiredService<ElasticsearchClient>() :
                    sp.GetRequiredKeyedService<ElasticsearchClient>(serviceKey)),
                failureStatus: null,
                tags: null,
                timeout: settings.HealthCheckTimeout > 0 ? TimeSpan.FromMilliseconds(settings.HealthCheckTimeout.Value) : null
                ));
        }

        ElasticsearchClient CreateElasticsearchClient(IServiceProvider serviceProvider)
        {
            var elasticsearchClientSettings = CreateElasticsearchClientSettings(settings, connectionName, DefaultConfigSectionName);

            configureClientSettings?.Invoke(elasticsearchClientSettings);

            return new ElasticsearchClient(elasticsearchClientSettings);
        }
    }

    private static ElasticsearchClientSettings CreateElasticsearchClientSettings(
        ElasticClientsElasticsearchSettings settings,
        string connectionName,
        string configurationSectionName)
    {
        if (settings.Endpoint is not null)
        {
            return new ElasticsearchClientSettings(settings.Endpoint);
        }
        else if (settings.CloudId is not null && settings.ApiKey is not null)
        {
            return new(settings.CloudId, new ApiKey(settings.ApiKey));
        }

        throw new InvalidOperationException(
                      $"A ElasticsearchClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                      $"{nameof(settings.Endpoint)} must be provided " +
                      $"in the '{configurationSectionName}' configuration section.");
    }
}
