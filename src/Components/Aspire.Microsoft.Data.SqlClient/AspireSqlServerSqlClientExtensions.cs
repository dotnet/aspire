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
    /// <param name="configure">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/>  is not provided in configuration section.</exception>
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, Action<MicrosoftDataSqlClientSettings>? configure = null)
        => AddSqlClient(builder, DefaultConfigSectionName, configure, name: null);

    /// <summary>
    /// Registers 'Scoped' <see cref="SqlConnection" /> factory for given <paramref name="name"/> for connecting Azure SQL, MsSQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configure">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/> is not provided in configuration section.</exception>
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, string name, Action<MicrosoftDataSqlClientSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        AddSqlClient(builder, $"{DefaultConfigSectionName}:{name}", configure, name);
    }

    private static void AddSqlClient(IHostApplicationBuilder builder, string configurationSectionName, Action<MicrosoftDataSqlClientSettings>? configure, string? name)
    {
        ArgumentNullException.ThrowIfNull(builder);

        MicrosoftDataSqlClientSettings configurationOptions = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            configurationOptions.ConnectionString = builder.Configuration.GetConnectionString("Aspire.SqlServer");
        }

        configure?.Invoke(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{configurationSectionName}' configuration section.");
        }

        if (string.IsNullOrEmpty(name))
        {
            builder.Services.AddScoped(_ => new SqlConnection(configurationOptions.ConnectionString));
        }
        else
        {
            builder.Services.AddKeyedScoped(name, (_, __) => new SqlConnection(configurationOptions.ConnectionString));
        }

        // SqlClient Data Provider (Microsoft.Data.SqlClient) handles connection pooling automatically and it's on by default
        // https://learn.microsoft.com/sql/connect/ado-net/sql-server-connection-pooling
        if (configurationOptions.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSqlClientInstrumentation();
            });
        }

        if (configurationOptions.Metrics)
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

        if (configurationOptions.HealthChecks)
        {
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    name is null ? "SqlServer" : $"SqlServer_{name}",
                    sp => new SqlServerHealthCheck(new SqlServerHealthCheckOptions()
                    {
                        ConnectionString = configurationOptions.ConnectionString
                    }),
                    failureStatus: default,
                    tags: default,
                    timeout: default));
        }
    }
}
