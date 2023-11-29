// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Oracle.ManagedDataAccess.Core;
using HealthChecks.Oracle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting Oracle database with Oracle.ManagedDataAccess.Core client
/// </summary>
public static class AspireOracleManagedDataAccessCoreExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Oracle:ManagedDataAccess:Core";

    /// <summary>
    /// Registers <see cref="OracleConnection"/> service for connecting Oracle database with Oracle.ManagedDataAccess.Core client.
    /// Configures health check and telemetry for the Oracle.ManagedDataAccess.Core client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Oracle:ManagedDataAccess:Core" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="OracleManagedDataAccessCoreSettings.ConnectionString"/> is not provided.</exception>
    public static void AddOracleManagedDataAccessCore(this IHostApplicationBuilder builder, string connectionName, Action<OracleManagedDataAccessCoreSettings>? configureSettings = null)
        => AddOracleManagedDataAccessCore(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="OracleConnection"/> as a keyed service for given <paramref name="name"/> for connecting Oracle database with Oracle.ManagedDataAccess.Core client.
    /// Configures health check and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Oracle:ManagedDataAccess:Core:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="OracleManagedDataAccessCoreSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedOracleManagedDataAccessCore(this IHostApplicationBuilder builder, string name, Action<OracleManagedDataAccessCoreSettings>? configureSettings = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddOracleManagedDataAccessCore(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddOracleManagedDataAccessCore(IHostApplicationBuilder builder, string configurationSectionName,
        Action<OracleManagedDataAccessCoreSettings>? configureSettings, string connectionName, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        OracleManagedDataAccessCoreSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        builder.RegisterOracleManagedDataAccessCoreServices(settings, configurationSectionName, connectionName, serviceKey);

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "OracleManagedDataAccessCore" : $"OracleManagedDataAccessCore_{connectionName}",
                sp => new OracleHealthCheck(new OracleHealthCheckOptions()
                {
                    ConnectionString = settings.ConnectionString ?? string.Empty
                }),
                failureStatus: default,
                tags: default,
                timeout: default));
        }

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddOracleDataProviderInstrumentation(o =>
                    {
                        o.EnableConnectionLevelAttributes = true;
                        o.RecordException = true;
                        o.InstrumentOracleDataReaderRead = true;
                        o.SetDbStatementForText = true;
                    })
                    .AddSource("Oracle.ManagedDataAccess.Core");
                });
        }
    }

    private static void RegisterOracleManagedDataAccessCoreServices(this IHostApplicationBuilder builder, OracleManagedDataAccessCoreSettings settings, string configurationSectionName, string connectionName, object? serviceKey)
    {
        if (serviceKey is null)
        {
            // delay validating the ConnectionString until the DataSource is requested. This ensures an exception doesn't happen until a Logger is established.
            builder.Services.AddScoped(serviceProvider =>
            {
                ValidateConnection();

                return new OracleConnection(settings.ConnectionString);
            });
        }
        else
        {
            builder.Services.AddKeyedScoped(serviceKey, (serviceProvider, key) =>
            {
                ValidateConnection();

                return new OracleConnection(settings.ConnectionString);
            });
        }

        void ValidateConnection()
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                throw new InvalidOperationException($"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{configurationSectionName}' configuration section.");
            }
        }
    }
}
