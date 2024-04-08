// Assembly 'Aspire.Confluent.Kafka'

using System;
using Aspire.Confluent.Kafka;
using Confluent.Kafka;

namespace Microsoft.Extensions.Hosting;

public static class AspireKafkaConsumerExtensions
{
    public static void AddKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null);
    public static void AddKeyedKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null);
}
