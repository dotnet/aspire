// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.MongoDB.Driver;

/// <summary>
/// Provides the client configuration settings for connecting to a MongoDB database using MongoDB driver.
/// </summary>
public sealed class MongoDBSettings
{
    /// <summary>
    /// The connection string of the MongoDB database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the MongoDB health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthCheckEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a integer value that indicates the MongoDB health check timeout in milliseconds.
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry tracing is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

}
