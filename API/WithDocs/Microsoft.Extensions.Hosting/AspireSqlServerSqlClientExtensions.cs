// Assembly 'Aspire.Microsoft.Data.SqlClient'

using System;
using Aspire.Microsoft.Data.SqlClient;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring SqlClient connection to Azure SQL, MS SQL server
/// </summary>
public static class AspireSqlServerSqlClientExtensions
{
    /// <summary>
    /// Registers 'Scoped' <see cref="T:Microsoft.Data.SqlClient.SqlConnection" /> factory for connecting Azure SQL, MS SQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">If required <see cref="P:Aspire.Microsoft.Data.SqlClient.MicrosoftDataSqlClientSettings.ConnectionString" />  is not provided in configuration section.</exception>
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, string connectionName, Action<MicrosoftDataSqlClientSettings>? configureSettings = null);

    /// <summary>
    /// Registers 'Scoped' <see cref="T:Microsoft.Data.SqlClient.SqlConnection" /> factory for given <paramref name="name" /> for connecting Azure SQL, MsSQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient:{name}" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">If required <see cref="P:Aspire.Microsoft.Data.SqlClient.MicrosoftDataSqlClientSettings.ConnectionString" /> is not provided in configuration section.</exception>
    public static void AddKeyedSqlServerClient(this IHostApplicationBuilder builder, string name, Action<MicrosoftDataSqlClientSettings>? configureSettings = null);
}
