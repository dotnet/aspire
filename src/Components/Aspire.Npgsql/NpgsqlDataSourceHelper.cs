// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HealthChecks.NpgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Aspire.Npgsql;

internal static class NpgsqlDataSourceHelper
{
    public static void AddNpgsqlDataSource<TSettings>(IHostApplicationBuilder builder,
        Action<TSettings>? configureSettings,
        string connectionName,
        object? serviceKey,
        string healthCheckPrefix,
        Func<IHostApplicationBuilder, string, TSettings> createSettings,
        Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder,
        Action<IHostApplicationBuilder, TSettings, string, object?, Action<NpgsqlDataSourceBuilder>?> registerNpgsqlServices) where TSettings : BaseNpgsqlSettings, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        TSettings settings = createSettings(builder, connectionName);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        registerNpgsqlServices(builder, settings, connectionName, serviceKey, configureDataSourceBuilder);

        // Same as SqlClient connection pooling is on by default and can be handled with connection string
        // https://www.npgsql.org/doc/connection-string-parameters.html#pooling
        if (!settings.DisableHealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? healthCheckPrefix : $"{healthCheckPrefix}_{connectionName}",
                sp => new NpgSqlHealthCheck(
                    new NpgSqlHealthCheckOptions(serviceKey is null
                        ? sp.GetRequiredService<NpgsqlDataSource>()
                        : sp.GetRequiredKeyedService<NpgsqlDataSource>(serviceKey))),
                failureStatus: default,
                tags: default,
                timeout: default));
        }

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddNpgsql();
                });
        }

        if (!settings.DisableMetrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(NpgsqlCommon.AddNpgsqlMetrics);
        }
    }
}
