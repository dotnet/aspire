// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Retry { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the database health check is enabled or not.</para>
    /// <para>The default value is <see langword="true"/>.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry tracing is enabled or not.</para>
    /// <para>The default value is <see langword="true"/>.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry metrics are enabled or not.</para>
    /// <para>The default value is <see langword="true"/>.</para>
    /// </summary>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// The time in seconds to wait for the command to execute.
    /// </summary>
    public int? Timeout { get; set; }
}
