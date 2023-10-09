// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Microsoft.Data.SqlClient;
using HealthChecks.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring SqlClient connection to Azure SQL, MS SQL server
/// </summary>
public static class AspireSqlServerSqlClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:Data:SqlClient";

    /// <summary>
    /// Registers 'Scoped' <see cref="SqlConnection" /> factory for connecting Azure SQL, MS SQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/>  is not provided in configuration section.</exception>
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, string connectionName, Action<MicrosoftDataSqlClientSettings>? configureSettings = null)
        => AddSqlClient(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);

    /// <summary>
    /// Registers 'Scoped' <see cref="SqlConnection" /> factory for given <paramref name="name"/> for connecting Azure SQL, MsSQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/> is not provided in configuration section.</exception>
    public static void AddKeyedSqlServerClient(this IHostApplicationBuilder builder, string name, Action<MicrosoftDataSqlClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        AddSqlClient(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddSqlClient(IHostApplicationBuilder builder, string configurationSectionName,
        Action<MicrosoftDataSqlClientSettings>? configure, string connectionName, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        MicrosoftDataSqlClientSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configure?.Invoke(settings);

        if (string.IsNullOrEmpty(settings.ConnectionString))
        {
            throw new InvalidOperationException($"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{configurationSectionName}' configuration section.");
        }

        if (serviceKey is null)
        {
            builder.Services.AddScoped(_ => new SqlConnection(settings.ConnectionString));
        }
        else
        {
            builder.Services.AddKeyedScoped(serviceKey, (_, __) => new SqlConnection(settings.ConnectionString));
        }

        // SqlClient Data Provider (Microsoft.Data.SqlClient) handles connection pooling automatically and it's on by default
        // https://learn.microsoft.com/sql/connect/ado-net/sql-server-connection-pooling
        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSqlClientInstrumentation();
            });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    // https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/SqlClientEventSource.cs#L73
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.Data.SqlClient.EventSource");
                });
            });
        }

        if (settings.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    serviceKey is null ? "SqlServer" : $"SqlServer_{connectionName}",
                    sp => new SqlServerHealthCheck(new SqlServerHealthCheckOptions()
                    {
                        ConnectionString = settings.ConnectionString
                    }),
                    failureStatus: default,
                    tags: default,
                    timeout: default));
        }
    }
}
