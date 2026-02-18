// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.ClickHouse.Driver;

/// <summary>
/// Provides the client configuration settings for connecting to a ClickHouse database using ClickHouse.Driver.
/// </summary>
public sealed class ClickHouseClientSettings
{
    /// <summary>
    /// The connection string of the ClickHouse database to connect to.
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
    /// The ClickHouse client currently does not produce metrics.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool DisableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for the health check.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null"/>, which uses the health check system default.
    /// </value>
    public TimeSpan? HealthCheckTimeout { get; set; }
}
