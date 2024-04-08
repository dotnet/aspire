// Assembly 'Aspire.Confluent.Kafka'

using System;
using Aspire.Confluent.Kafka;
using Confluent.Kafka;

namespace Microsoft.Extensions.Hosting;

public static class AspireKafkaProducerExtensions
{
    public static void AddKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null);
    public static void AddKeyedKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null);
}
