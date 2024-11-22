// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.RabbitMQ.Client;

/// <summary>
/// Provides the client configuration settings for connecting to a RabbitMQ message broker.
/// </summary>
public sealed class RabbitMQClientSettings
{
    /// <summary>
    /// Gets or sets the connection string of the RabbitMQ server to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// <para>Gets or sets the maximum number of connection retry attempts.</para>
    /// <para>Default value is 5, set it to 0 to disable the retry mechanism.</para>
    /// </summary>
    public int MaxConnectRetryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the RabbitMQ health check is disabled or not.
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
}
