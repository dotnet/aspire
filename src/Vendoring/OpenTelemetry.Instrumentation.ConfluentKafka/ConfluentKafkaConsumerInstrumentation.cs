// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Confluent.Kafka;

namespace OpenTelemetry.Instrumentation.ConfluentKafka;

internal class ConfluentKafkaConsumerInstrumentation;

#pragma warning disable SA1402 // File may only contain a single type
internal sealed class ConfluentKafkaConsumerInstrumentation<TKey, TValue> : ConfluentKafkaConsumerInstrumentation
#pragma warning restore SA1402 // File may only contain a single type
{
    public ConfluentKafkaConsumerInstrumentation(InstrumentedConsumerBuilder<TKey, TValue> consumerBuilder)
    {
        this.ConsumerBuilder = consumerBuilder;
    }

    internal InstrumentedConsumerBuilder<TKey, TValue> ConsumerBuilder { get; }
}
