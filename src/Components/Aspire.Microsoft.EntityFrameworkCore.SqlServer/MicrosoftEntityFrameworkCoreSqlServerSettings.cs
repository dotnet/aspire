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
    /// Gets or sets a boolean value that indicates whether the DbContext will be pooled or explicitly created every time it's requested.
    /// </summary>
    public bool DbContextPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    /// <value>
    /// The default is 6.
    /// Set it to 0 to disable the retry mechanism.
    /// </value>
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the database health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// The time in seconds to wait for the command to execute.
    /// </summary>
    public int? Timeout { get; set; }
}
