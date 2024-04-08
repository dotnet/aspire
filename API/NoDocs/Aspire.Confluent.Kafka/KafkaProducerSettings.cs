// Assembly 'Aspire.Confluent.Kafka'

using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

public sealed class KafkaProducerSettings
{
    public string? ConnectionString { get; set; }
    public ProducerConfig Config { get; }
    public bool Metrics { get; set; }
    public bool HealthChecks { get; set; }
    public KafkaProducerSettings();
}
