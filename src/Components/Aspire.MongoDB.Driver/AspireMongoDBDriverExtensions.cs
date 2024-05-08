// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting MongoDB database with MongoDB.Driver client.
/// </summary>
public static class AspireMongoDBDriverExtensions
{
    private const string DefaultConfigSectionName = "Aspire:MongoDB:Driver";
    private const string ActivityNameSource = "MongoDB.Driver.Core.Extensions.DiagnosticSources";

    /// <summary>
    /// Registers <see cref="IMongoClient"/> and <see cref="IMongoDatabase"/> instances for connecting MongoDB database with MongoDB.Driver client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MongoDB:Driver" section.</remarks>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing MongoClientSettings.</param>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="MongoDBSettings.ConnectionString"/> is not provided.</exception>
    public static void AddMongoDBClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<MongoDBSettings>? configureSettings = null,
        Action<MongoClientSettings>? configureClientSettings = null)
        => builder.AddMongoDBClient(DefaultConfigSectionName, configureSettings, configureClientSettings, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="IMongoClient"/> and <see cref="IMongoDatabase"/> instances for connecting MongoDB database with MongoDB.Driver client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MongoDB:Driver:{name}" section.</remarks>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing MongoClientSettings.</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="MongoDBSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedMongoDBClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<MongoDBSettings>? configureSettings = null,
        Action<MongoClientSettings>? configureClientSettings = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddMongoDBClient(
            $"{DefaultConfigSectionName}:{name}",
            configureSettings,
            configureClientSettings,
            connectionName: name,
            serviceKey: name);
    }

    private static void AddMongoDBClient(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<MongoDBSettings>? configureSettings,
        Action<MongoClientSettings>? configureClientSettings,
        string connectionName,
        object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetMongoDBSettings(
            connectionName,
            configurationSectionName,
            configureSettings);

        builder.AddMongoClient(
            settings,
            connectionName,
            configurationSectionName,
            configureClientSettings,
            serviceKey);

        if (!settings.DisableTracing)
        {
            builder.Services
                .AddOpenTelemetry()
                .WithTracing(tracer => tracer.AddSource(ActivityNameSource));
        }

        builder.AddMongoDatabase(settings.ConnectionString, serviceKey);
        builder.AddHealthCheck(
            serviceKey is null ? "MongoDB.Driver" : $"MongoDB.Driver_{connectionName}",
            settings);
    }

    private static void AddMongoClient(
        this IHostApplicationBuilder builder,
        MongoDBSettings mongoDbSettings,
        string connectionName,
        string configurationSectionName,
        Action<MongoClientSettings>? configureClientSettings,
        object? serviceKey)
    {
        if (serviceKey is null)
        {
            builder
                .Services
                .AddSingleton<IMongoClient>(sp => sp.CreateMongoClient(connectionName, configurationSectionName, mongoDbSettings, configureClientSettings));
            return;
        }

        builder
            .Services
            .AddKeyedSingleton<IMongoClient>(serviceKey, (sp, _) => sp.CreateMongoClient(connectionName, configurationSectionName, mongoDbSettings, configureClientSettings));
    }

    private static void AddMongoDatabase(
        this IHostApplicationBuilder builder,
        string? connectionString,
        object? serviceKey = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var mongoUrl = MongoUrl.Create(connectionString);

        if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
        {
            return;
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<IMongoDatabase>(provider =>
            {
                return provider
                    .GetRequiredService<IMongoClient>()
                    .GetDatabase(mongoUrl.DatabaseName);
            });

            return;
        }

        builder.Services.AddKeyedSingleton<IMongoDatabase>(serviceKey, (provider, _) =>
        {
            return provider
                .GetRequiredKeyedService<IMongoClient>(serviceKey)
                .GetDatabase(mongoUrl.DatabaseName);
        });
    }

    private static void AddHealthCheck(
        this IHostApplicationBuilder builder,
        string healthCheckName,
        MongoDBSettings settings)
    {
        if (settings.DisableHealthChecks || string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return;
        }

        builder.TryAddHealthCheck(
            healthCheckName,
            healthCheck => healthCheck.AddMongoDb(
                settings.ConnectionString,
                healthCheckName,
                null,
                null,
                settings.HealthCheckTimeout > 0 ? TimeSpan.FromMilliseconds(settings.HealthCheckTimeout.Value) : null));
    }

    private static MongoClient CreateMongoClient(
        this IServiceProvider serviceProvider,
        string connectionName,
        string configurationSectionName,
        MongoDBSettings mongoDbSettings,
        Action<MongoClientSettings>? configureClientSettings)
    {
        mongoDbSettings.ValidateSettings(connectionName, configurationSectionName);

        var clientSettings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);

        if (!mongoDbSettings.DisableTracing)
        {
            clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        }

        configureClientSettings?.Invoke(clientSettings);

        clientSettings.LoggingSettings ??= new LoggingSettings(serviceProvider.GetService<ILoggerFactory>());

        return new MongoClient(clientSettings);
    }

    private static MongoDBSettings GetMongoDBSettings(
        this IHostApplicationBuilder builder,
        string connectionName,
        string configurationSectionName,
        Action<MongoDBSettings>? configureSettings)
    {
        var settings = new MongoDBSettings();

        builder.Configuration
            .GetSection(configurationSectionName)
            .Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        return settings;
    }

    private static void ValidateSettings(
        this MongoDBSettings settings,
        string connectionName,
        string configurationSectionName)
    {
        ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, configurationSectionName);
    }
}
