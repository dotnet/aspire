// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire;
using Aspire.Npgsql;
using HealthChecks.NpgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting PostgreSQL database with Npgsql client
/// </summary>
public static class AspirePostgreSqlNpgsqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Npgsql";

    /// <summary>
    /// Registers <see cref="NpgsqlDataSource"/> service for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="NpgsqlDataSourceBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
        => AddNpgsqlDataSource(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null, configureDataSourceBuilder: configureDataSourceBuilder);

    /// <summary>
    /// Registers <see cref="NpgsqlDataSource"/> as a keyed service for given <paramref name="name"/> for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="NpgsqlDataSourceBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddNpgsqlDataSource(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name, configureDataSourceBuilder: configureDataSourceBuilder);
    }

    private static void AddNpgsqlDataSource(IHostApplicationBuilder builder, string configurationSectionName,
        Action<NpgsqlSettings>? configureSettings, string connectionName, object? serviceKey, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        NpgsqlSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        builder.RegisterNpgsqlServices(settings, configurationSectionName, connectionName, serviceKey, configureDataSourceBuilder);

        // Same as SqlClient connection pooling is on by default and can be handled with connection string 
        // https://www.npgsql.org/doc/connection-string-parameters.html#pooling
        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "PostgreSql" : $"PostgreSql_{connectionName}",
                sp => new NpgSqlHealthCheck(
                    new NpgSqlHealthCheckOptions(serviceKey is null
                        ? sp.GetRequiredService<NpgsqlDataSource>()
                        : sp.GetRequiredKeyedService<NpgsqlDataSource>(serviceKey))),
                failureStatus: default,
                tags: default,
                timeout: default));
        }

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddNpgsql();
                });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(NpgsqlCommon.AddNpgsqlMetrics);
        }
    }

    private static void RegisterNpgsqlServices(this IHostApplicationBuilder builder, NpgsqlSettings settings, string configurationSectionName, string connectionName, object? serviceKey, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        if (serviceKey is null)
        {
            // delay validating the ConnectionString until the DataSource is requested. This ensures an exception doesn't happen until a Logger is established.
            builder.Services.AddNpgsqlDataSource(settings.ConnectionString ?? string.Empty, dataSourceBuilder =>
            {
                ValidateConnection();

                configureDataSourceBuilder?.Invoke(dataSourceBuilder);
            });
        }
        else
        {
            // Currently Npgsql does not support Keyed DI Registration, so we implement it on our own.
            // Register a NpgsqlDataSource factory method, based on https://github.com/npgsql/npgsql/blob/c2fc02a858176f2b5eab7a2c2336ff5ab4748ad0/src/Npgsql.DependencyInjection/NpgsqlServiceCollectionExtensions.cs#L147-L150
            builder.Services.AddKeyedSingleton<NpgsqlDataSource>(serviceKey, (serviceProvider, _) =>
            {
                ValidateConnection();

                var dataSourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
                dataSourceBuilder.UseLoggerFactory(serviceProvider.GetService<ILoggerFactory>());

                configureDataSourceBuilder?.Invoke(dataSourceBuilder);

                return dataSourceBuilder.Build();
            });
            // Common Services, based on https://github.com/npgsql/npgsql/blob/c2fc02a858176f2b5eab7a2c2336ff5ab4748ad0/src/Npgsql.DependencyInjection/NpgsqlServiceCollectionExtensions.cs#L165
            // They let the users resolve NpgsqlConnection directly.
            builder.Services.AddKeyedSingleton<DbDataSource>(serviceKey, static (serviceProvider, key) => serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(key));
            builder.Services.AddKeyedTransient<NpgsqlConnection>(serviceKey, static (serviceProvider, key) => serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(key).CreateConnection());
            builder.Services.AddKeyedTransient<DbConnection>(serviceKey, static (serviceProvider, key) => serviceProvider.GetRequiredKeyedService<NpgsqlConnection>(key));
        }

        void ValidateConnection()
        {
            ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, configurationSectionName);
        }
    }
}
