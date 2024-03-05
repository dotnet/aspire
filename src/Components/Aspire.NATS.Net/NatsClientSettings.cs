// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.NATS.Net;

/// <summary>
/// Provides the client configuration settings for connecting to a NATS cluster.
/// </summary>
public sealed class NatsClientSettings
{
    /// <summary>
    /// Gets or sets the connection string of the NATS cluster to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the NATS health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool HealthChecks { get; set; } = true;
}
