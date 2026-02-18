// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.ClickHouse.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ClickHouseDataSource = ClickHouse.Driver.ADO.ClickHouseDataSource;
using ClickHouseConnectionStringBuilder = ClickHouse.Driver.ADO.ClickHouseConnectionStringBuilder;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting ClickHouse database with ClickHouse.Driver.
/// </summary>
public static class AspireClickHouseExtensions
{
    private const string DefaultConfigSectionName = "Aspire:ClickHouse:Driver";
    private const string ActivitySourceName = "ClickHouse.Driver";

    /// <summary>
    /// Registers <see cref="ClickHouseDataSource"/> service for connecting to a ClickHouse database with ClickHouse.Driver.
    /// Configures health check, logging and telemetry for the ClickHouse client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionString">An optional delegate that can be used for customizing the <see cref="ClickHouseConnectionStringBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:ClickHouse:Driver" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ClickHouseClientSettings.ConnectionString"/> is not provided.</exception>
    public static void AddClickHouseDataSource(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<ClickHouseClientSettings>? configureSettings = null,
        Action<ClickHouseConnectionStringBuilder>? configureConnectionString = null)
        => AddClickHouseDataSourceCore(builder, configureSettings, connectionName, serviceKey: null, configureConnectionString: configureConnectionString);

    /// <summary>
    /// Registers <see cref="ClickHouseDataSource"/> as a keyed service for given <paramref name="name"/> for connecting to a ClickHouse database with ClickHouse.Driver.
    /// Configures health check, logging and telemetry for the ClickHouse client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionString">An optional delegate that can be used for customizing the <see cref="ClickHouseConnectionStringBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:ClickHouse:Driver:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ClickHouseClientSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedClickHouseDataSource(
        this IHostApplicationBuilder builder,
        string name,
        Action<ClickHouseClientSettings>? configureSettings = null,
        Action<ClickHouseConnectionStringBuilder>? configureConnectionString = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddClickHouseDataSourceCore(builder, configureSettings, connectionName: name, serviceKey: name, configureConnectionString: configureConnectionString);
    }

    private static void AddClickHouseDataSourceCore(
        IHostApplicationBuilder builder,
        Action<ClickHouseClientSettings>? configureSettings,
        string connectionName,
        object? serviceKey,
        Action<ClickHouseConnectionStringBuilder>? configureConnectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        // 1. Configuration binding
        ClickHouseClientSettings settings = new();
        var configSection = builder.Configuration.GetSection(DefaultConfigSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);
        configSection.Bind(settings);
        namedConfigSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        // 2. DI registration using the driver's settingsFactory overload
        builder.Services.AddClickHouseDataSource(
            sp =>
            {
                // Deferred validation — connection string validated when DataSource is first resolved
                ConnectionStringValidation.ValidateConnectionString(
                    settings.ConnectionString,
                    connectionName,
                    DefaultConfigSectionName);

                var connectionStringBuilder = new ClickHouseConnectionStringBuilder();

                if (!string.IsNullOrEmpty(settings.ConnectionString))
                {
                    connectionStringBuilder.ConnectionString = settings.ConnectionString;
                }

                configureConnectionString?.Invoke(connectionStringBuilder);

                var driverSettings = connectionStringBuilder.ToSettings();
                return new ClickHouse.Driver.ADO.ClickHouseClientSettings(driverSettings)
                {
                    LoggerFactory = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
                };
            },
            serviceKey: serviceKey);

        // 3. Health check registration
        if (!settings.DisableHealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "ClickHouse" : $"ClickHouse_{connectionName}",
                sp => new ClickHouseHealthCheck(
                    serviceKey is null
                        ? sp.GetRequiredService<ClickHouseDataSource>()
                        : sp.GetRequiredKeyedService<ClickHouseDataSource>(serviceKey)),
                failureStatus: default,
                tags: default,
                timeout: settings.HealthCheckTimeout));
        }

        // 4. Tracing — the driver emits activities via ActivitySource("ClickHouse.Driver")
        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddSource(ActivitySourceName);
                });
        }

        // Driver doesn't emit System.Diagnostics.Metric
    }
}
