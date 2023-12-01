// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Pomelo.EntityFrameworkCore.MySql;

/// <summary>
/// Provides the client configuration settings for connecting to a MySQL database using EntityFrameworkCore.
/// </summary>
public sealed class PomeloEntityFrameworkCoreMySqlSettings
{
    /// <summary>
    /// Gets or sets the connection string of the MySQL database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the server version of the MySQL database to connect to.
    /// </summary>
    public string? ServerVersion { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the DbContext will be pooled or explicitly created every time it's requested.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    /// <remarks>Should be set to false in multi-tenant scenarios.</remarks>
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
