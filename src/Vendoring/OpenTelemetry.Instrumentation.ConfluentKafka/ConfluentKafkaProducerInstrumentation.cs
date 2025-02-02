// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Confluent.Kafka;

namespace OpenTelemetry.Instrumentation.ConfluentKafka;

internal abstract class ConfluentKafkaProducerInstrumentation;

#pragma warning disable SA1402 // File may only contain a single type
internal sealed class ConfluentKafkaProducerInstrumentation<TKey, TValue> : ConfluentKafkaProducerInstrumentation
#pragma warning restore SA1402 // File may only contain a single type
{
    public ConfluentKafkaProducerInstrumentation(InstrumentedProducerBuilder<TKey, TValue> producerBuilder)
    {
        this.ProducerBuilder = producerBuilder;
    }

    internal InstrumentedProducerBuilder<TKey, TValue> ProducerBuilder { get; }
}
