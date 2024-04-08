// Assembly 'Aspire.Oracle.EntityFrameworkCore'

using System.Runtime.CompilerServices;

namespace Aspire.Oracle.EntityFrameworkCore;

/// <summary>
/// Provides the client configuration settings for connecting to a Oracle database using EntityFrameworkCore.
/// </summary>
public sealed class OracleEntityFrameworkCoreSettings
{
    /// <summary>
    /// The connection string of the Oracle database to connect to.
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
    /// <para>Gets or sets a boolean value that indicates whether the database health check is enabled or not.</para>
    /// <para>The default value is <see langword="true" />.</para>
    /// </summary>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets the time in seconds to wait for the command to execute.
    /// </summary>
    public int? CommandTimeout { get; set; }

    public OracleEntityFrameworkCoreSettings();
}
