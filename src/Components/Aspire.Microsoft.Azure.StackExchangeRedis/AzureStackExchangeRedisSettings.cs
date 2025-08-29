// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Microsoft.Azure.StackExchangeRedis;

/// <summary>
/// Provides the client configuration settings for connecting to an Azure Cache for Redis using StackExchange.Redis.
/// </summary>
public sealed class AzureStackExchangeRedisSettings
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
    /// Gets or sets the credential used to authenticate to the Azure Cache for Redis.
    /// </summary>
    public TokenCredential? Credential { get; set; }
}