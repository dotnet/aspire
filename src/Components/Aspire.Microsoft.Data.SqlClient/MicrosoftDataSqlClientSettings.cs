// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// <para>Gets or sets a boolean value that indicates whether the database health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Metrics { get; set; } = true;
}

