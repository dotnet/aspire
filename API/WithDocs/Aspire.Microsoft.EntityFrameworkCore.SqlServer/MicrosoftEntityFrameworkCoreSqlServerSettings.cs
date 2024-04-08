// Assembly 'Aspire.Microsoft.EntityFrameworkCore.SqlServer'

using System.Runtime.CompilerServices;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer;

/// <summary>
/// Provides the client configuration settings for connecting to a SQL Server database using EntityFrameworkCore.
/// </summary>
public sealed class MicrosoftEntityFrameworkCoreSqlServerSettings
{
    /// <summary>
    /// The connection string of the SQL server database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether retries should be enabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Retry { get; set; }

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

    /// <summary>
    /// Gets or sets the time in seconds to wait for the command to execute.
    /// </summary>
    public int? CommandTimeout { get; set; }

    public MicrosoftEntityFrameworkCoreSqlServerSettings();
}
