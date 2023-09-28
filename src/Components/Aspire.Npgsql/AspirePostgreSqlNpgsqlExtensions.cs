// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Npgsql;
using HealthChecks.NpgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting PostgreSQL database with Npgsql client
/// </summary>
public static class AspirePostgreSqlNpgsqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Npgsql";

    /// <summary>
    /// Registers <see cref="NpgsqlDataSource" /> service for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configure">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, Action<NpgsqlSettings>? configure = null)
        => AddNpgsqlDataSource(builder, DefaultConfigSectionName, configure, name: null);

    /// <summary>
    /// Registers <see cref="NpgsqlDataSource" /> service for given <paramref name="name"/> for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configure">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<NpgsqlSettings>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddNpgsqlDataSource(builder, $"{DefaultConfigSectionName}:{name}", configure, name);
    }

    private static void AddNpgsqlDataSource(IHostApplicationBuilder builder, string configurationSectionName,
        Action<NpgsqlSettings>? configure, string? name)
    {
        ArgumentNullException.ThrowIfNull(builder);

        NpgsqlSettings configurationOptions = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            configurationOptions.ConnectionString = builder.Configuration.GetConnectionString("Aspire.PostgreSQL");
        }

        configure?.Invoke(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{configurationSectionName}' configuration section.");
        }

        if (name is null)
        {
            builder.Services.AddNpgsqlDataSource(configurationOptions.ConnectionString);
        }
        else
        {
            // Currently Npgsql does not support Keyed DI Registration, so we implement it on our own.
            // Register a NpgsqlDataSource factory method, based on https://github.com/npgsql/npgsql/blob/c2fc02a858176f2b5eab7a2c2336ff5ab4748ad0/src/Npgsql.DependencyInjection/NpgsqlServiceCollectionExtensions.cs#L147-L150
            builder.Services.AddKeyedSingleton<NpgsqlDataSource>(name, (serviceProvider, _) =>
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(configurationOptions.ConnectionString);
                dataSourceBuilder.UseLoggerFactory(serviceProvider.GetService<ILoggerFactory>());
                return dataSourceBuilder.Build();
            });
            // Common Services, based on https://github.com/npgsql/npgsql/blob/c2fc02a858176f2b5eab7a2c2336ff5ab4748ad0/src/Npgsql.DependencyInjection/NpgsqlServiceCollectionExtensions.cs#L165
            // They let the users resolve NpgsqlConnection directly.
            builder.Services.AddKeyedSingleton<DbDataSource>(name, (serviceProvider, _) => serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(name));
            builder.Services.AddKeyedSingleton<NpgsqlConnection>(name, (serviceProvider, _) => serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(name).CreateConnection());
            builder.Services.AddKeyedSingleton<DbConnection>(name, (serviceProvider, _) => serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(name).CreateConnection());
        }

        // Same as SqlClient connection pooling is on by default and can be handled with connection string 
        // https://www.npgsql.org/doc/connection-string-parameters.html#pooling
        if (configurationOptions.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    name is null ? "PostgreSql" : $"PostgreSql_{name}",
                    sp => new NpgSqlHealthCheck(new NpgSqlHealthCheckOptions()
                    {
                        DataSource = name is null
                            ? sp.GetRequiredService<NpgsqlDataSource>()
                            : sp.GetRequiredKeyedService<NpgsqlDataSource>(name)
                    }),
                    failureStatus: default,
                    tags: default,
                    timeout: default));
        }

        if (configurationOptions.Tracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddNpgsql();
                });
        }

        if (configurationOptions.Metrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(meterProviderBuilder =>
                {
                    // https://www.npgsql.org/doc/diagnostics/metrics.html?q=metrics
                    meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                    {
                        // https://github.com/npgsql/npgsql/blob/b3282aa6124184162b66dd4ab828041f872bc602/src/Npgsql/NpgsqlEventSource.cs#L14
                        eventCountersInstrumentationOptions.AddEventSources("Npgsql");
                    });
                });
        }
    }
}
