// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

/// <summary>
/// Metrics are emitted using json data published by librdkafka StatisticsHandler (see https://docs.confluent.io/platform/current/clients/confluent-kafka-dotnet/_site/api/Confluent.Kafka.ConsumerBuilder-2.html#Confluent_Kafka_ConsumerBuilder_2_StatisticsHandler)
/// The <see cref="MetricsChannel"/> is written by both StatisticsHandler of <see cref="IConsumer{TKey,TValue}"/> and <see cref="IProducer{TKey,TValue}"/> and aims
/// to avoid slowing down <see cref="IConsumer{TKey,TValue}"/>'s consume thread and <see cref="IProducer{TKey,TValue}"/>'s poll thread by offloading the processing of the json.
/// The json processing is performed by <see cref="MetricsService"/>.
/// </summary>
internal sealed class MetricsChannel
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10_000)
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<string> Reader => _channel.Reader;
    public ChannelWriter<string> Writer => _channel.Writer;
}
