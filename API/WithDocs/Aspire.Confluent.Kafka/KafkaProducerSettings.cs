// Assembly 'Aspire.Confluent.Kafka'

using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

/// <summary>
/// Provides the client configuration settings for connecting to a Kafka message broker to produce messages.
/// </summary>
public sealed class KafkaProducerSettings
{
    /// <summary>
    /// Gets or sets the connection string of the Kafka server to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the configuration settings for the Kafka producer.
    /// </summary>
    public ProducerConfig Config { get; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether collecting metrics is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Metrics { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Kafka health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    public KafkaProducerSettings();
}
