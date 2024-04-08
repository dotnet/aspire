// Assembly 'Aspire.MySqlConnector'

using System;
using Aspire.MySqlConnector;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting MySQL database with MySqlConnector client
/// </summary>
public static class AspireMySqlConnectorExtensions
{
    /// <summary>
    /// Registers <see cref="T:MySqlConnector.MySqlDataSource" /> service for connecting MySQL database with MySqlConnector client.
    /// Configures health check, logging and telemetry for the MySqlConnector client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MySqlConnector" section.</remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.MySqlConnector.MySqlConnectorSettings.ConnectionString" /> is not provided.</exception>
    public static void AddMySqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<MySqlConnectorSettings>? configureSettings = null);

    /// <summary>
    /// Registers <see cref="T:MySqlConnector.MySqlDataSource" /> as a keyed service for given <paramref name="name" /> for connecting MySQL database with MySqlConnector client.
    /// Configures health check, logging and telemetry for the MySqlConnector client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MySqlConnector:{name}" section.</remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="builder" /> or <paramref name="name" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">Thrown if mandatory <paramref name="name" /> is empty.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.MySqlConnector.MySqlConnectorSettings.ConnectionString" /> is not provided.</exception>
    public static void AddKeyedMySqlDataSource(this IHostApplicationBuilder builder, string name, Action<MySqlConnectorSettings>? configureSettings = null);
}
