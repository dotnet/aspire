// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.StackExchange.Redis;

/// <summary>
/// Provides the client configuration settings for connecting to a Redis server.
/// </summary>
public sealed class StackExchangeRedisSettings
{
    /// <summary>
    /// Gets or sets the comma-delimited configuration string used to connect to the Redis server.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Redis health check is disabled or not.
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
    /// Gets or sets a boolean value that indicates whether auto activation is disabled or not.
    /// </summary>
    /// <remarks>
    /// When auto activation is enabled, the Redis connection is established at startup time rather than on first use,
    /// which prevents blocking threads when the connection is first requested from the DI container.
    /// </remarks>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool DisableAutoActivation { get; set; } = true;
}
