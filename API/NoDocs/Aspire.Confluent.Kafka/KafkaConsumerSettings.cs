// Assembly 'Aspire.Confluent.Kafka'

using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

public sealed class KafkaConsumerSettings
{
    public string? ConnectionString { get; set; }
    public ConsumerConfig Config { get; }
    public bool Metrics { get; set; }
    public bool HealthChecks { get; set; }
    public KafkaConsumerSettings();
}
