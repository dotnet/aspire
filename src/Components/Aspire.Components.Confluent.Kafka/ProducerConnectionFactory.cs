// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

internal sealed class ProducerConnectionFactory<TKey, TValue>
{
    private readonly ProducerBuilder<TKey, TValue> _producerBuilder;
    private readonly ProducerConfig _producerConfig;

    public ProducerConnectionFactory(ProducerBuilder<TKey, TValue> producerBuilder, ProducerConfig producerConfig)
    {
        _producerConfig = new ProducerConfig();
        foreach (var property in producerConfig)
        {
            _producerConfig.Set(property.Key, property.Value);
        }
        _producerBuilder = producerBuilder;
    }

    public ProducerConfig Config => _producerConfig;

    public IProducer<TKey, TValue> Create() => _producerBuilder.Build();
}
