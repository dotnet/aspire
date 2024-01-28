// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;

/// <summary>
/// Provides the client configuration settings for connecting to a PostgreSQL database using EntityFrameworkCore.
/// </summary>
public sealed class NpgsqlEntityFrameworkCorePostgreSQLSettings
{
    /// <summary>
    /// Gets or sets the connection string of the PostgreSQL database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the DbContext will be pooled or explicitly created every time it's requested.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    /// <remarks>Should be set to false in multi-tenant scenarios.</remarks>
    public bool DbContextPooling { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets the maximum number of retry attempts.</para>
    /// <para>Default value is 6, set it to 0 to disable the retry mechanism.</para>
    /// </summary>
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
}
