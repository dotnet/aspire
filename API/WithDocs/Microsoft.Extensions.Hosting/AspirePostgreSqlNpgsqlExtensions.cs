// Assembly 'Aspire.Npgsql'

using System;
using Aspire.Npgsql;
using Npgsql;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting PostgreSQL database with Npgsql client
/// </summary>
public static class AspirePostgreSqlNpgsqlExtensions
{
    /// <summary>
    /// Registers <see cref="T:Npgsql.NpgsqlDataSource" /> service for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="T:Npgsql.NpgsqlDataSourceBuilder" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql" section.</remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Npgsql.NpgsqlSettings.ConnectionString" /> is not provided.</exception>
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Npgsql.NpgsqlDataSource" /> as a keyed service for given <paramref name="name" /> for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="T:Npgsql.NpgsqlDataSourceBuilder" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql:{name}" section.</remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="builder" /> or <paramref name="name" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">Thrown if mandatory <paramref name="name" /> is empty.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Npgsql.NpgsqlSettings.ConnectionString" /> is not provided.</exception>
    public static void AddKeyedNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null);
}
