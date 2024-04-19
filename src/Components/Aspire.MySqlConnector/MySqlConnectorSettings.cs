// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Gets or sets a boolean value that indicates whether the database health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableMetrics { get; set; }
}
