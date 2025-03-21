// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="BaseNpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
        => NpgsqlDataSourceHelper.AddNpgsqlDataSource(builder, configureSettings, connectionName, serviceKey: null, healthCheckPrefix: "PostgreSql", CreateNpgsqlSettings, configureDataSourceBuilder: configureDataSourceBuilder, RegisterNpgsqlServices);

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
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="BaseNpgsqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        NpgsqlDataSourceHelper.AddNpgsqlDataSource(builder, configureSettings, connectionName: name, serviceKey: name, healthCheckPrefix: "PostgreSql", CreateNpgsqlSettings, configureDataSourceBuilder: configureDataSourceBuilder, RegisterNpgsqlServices);
    }

    private static NpgsqlSettings CreateNpgsqlSettings(IHostApplicationBuilder builder, string connectionName)
    {
        NpgsqlSettings settings = new();
        var configSection = builder.Configuration.GetSection(DefaultConfigSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);
        configSection.Bind(settings);
        namedConfigSection.Bind(settings);

        return settings;
    }

    private static void RegisterNpgsqlServices(IHostApplicationBuilder builder, NpgsqlSettings settings, string connectionName, object? serviceKey, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        builder.Services.AddNpgsqlDataSource(settings.ConnectionString ?? string.Empty, dataSourceBuilder =>
        {
            // delay validating the ConnectionString until the DataSource is requested. This ensures an exception doesn't happen until a Logger is established.
            ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName);

            configureDataSourceBuilder?.Invoke(dataSourceBuilder);
        }, serviceKey: serviceKey);
    }
}
