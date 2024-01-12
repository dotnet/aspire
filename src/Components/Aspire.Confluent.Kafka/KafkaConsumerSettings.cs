// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

/// <summary>
/// Provides the client configuration settings for connecting to a Kafka message broker to consume messages.
/// </summary>
public sealed class KafkaConsumerSettings
{
    /// <summary>
    /// Gets or sets the connection string of the Kafka server to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the configuration settings for the Kafka consumer.
    /// </summary>
    public ConsumerConfig Config { get; } = new ConsumerConfig();

    /// <summary>
    /// Gets or sets a boolean value that indicates whether collecting metrics is enabled or not.
    /// </summary>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Kafka health check is enabled or not.
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    internal void Consolidate()
    {
        Debug.Assert(Config is not null);

        if (ConnectionString is not null)
        {
            Config.BootstrapServers = ConnectionString;
        }

        if (Metrics)
        {
            Config.StatisticsIntervalMs ??= 1000;
        }
    }

    internal void Validate()
    {
        if (Config.BootstrapServers is null)
        {
            throw new InvalidOperationException("No bootstrap servers configured.");
        }

        if (Config.GroupId is null)
        {
            throw new InvalidOperationException("No group id configured.");
        }
    }
}
