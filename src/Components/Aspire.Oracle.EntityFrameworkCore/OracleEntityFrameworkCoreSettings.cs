// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Oracle.ManagedDataAccess.OpenTelemetry;

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
    /// Gets or sets whether retries should be disabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableRetry { get; set; }

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
    /// Gets or sets the time in seconds to wait for the command to execute.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets an action to modify the default Open Telemetry instrumentation options
    /// </summary>
    public Action<OracleDataProviderInstrumentationOptions>? InstrumentationOptions { get; set; }
}
