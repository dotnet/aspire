// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Gets or sets a boolean value that indicates whether the db context will be pooled or explicitly created every time it's requested.
    /// </summary>
    public bool DbContextPooling { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets the maximum number of retry attempts.</para>
    /// <para>Default value is 6, set it to 0 to disable the retry mechanism.</para>
    /// </summary>
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the database health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry tracing is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry metrics are enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// The time in seconds to wait for the command to execute.
    /// </summary>
    public int? Timeout { get; set; }
}
