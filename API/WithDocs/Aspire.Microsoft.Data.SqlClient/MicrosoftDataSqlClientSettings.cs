// Assembly 'Aspire.Microsoft.Data.SqlClient'

using System.Runtime.CompilerServices;

namespace Aspire.Microsoft.Data.SqlClient;

/// <summary>
/// Provides the client configuration settings for connecting to a SQL Server database using SqlClient.
/// </summary>
public sealed class MicrosoftDataSqlClientSettings
{
    /// <summary>
    /// Gets or sets the connection string of the SQL Server database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the database health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public MicrosoftDataSqlClientSettings();
}
