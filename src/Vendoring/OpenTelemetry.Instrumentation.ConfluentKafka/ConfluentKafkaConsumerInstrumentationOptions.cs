// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Instrumentation.ConfluentKafka;

internal class ConfluentKafkaConsumerInstrumentationOptions<TKey, TValue>
{
    public bool Metrics { get; set; }

    public bool Traces { get; set; }
}
