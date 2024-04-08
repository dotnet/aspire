// Assembly 'Aspire.MySqlConnector'

using System.Runtime.CompilerServices;

namespace Aspire.MySqlConnector;

/// <summary>
/// Provides the client configuration settings for connecting to a MySQL database using MySqlConnector.
/// </summary>
public sealed class MySqlConnectorSettings
{
    /// <summary>
    /// The connection string of the MySQL database to connect to.
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

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Metrics { get; set; }

    public MySqlConnectorSettings();
}
