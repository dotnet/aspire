// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

internal sealed class ConsumerConnectionFactory<TKey, TValue>
{
    private readonly ConsumerBuilder<TKey, TValue> _consumerBuilder;
    private readonly ConsumerConfig _consumerConfig;

    public ConsumerConnectionFactory(ConsumerBuilder<TKey, TValue> consumerBuilder, ConsumerConfig consumerConfig)
    {
        _consumerConfig = new ConsumerConfig();
        foreach (var property in consumerConfig)
        {
            _consumerConfig.Set(property.Key, property.Value);
        }
        _consumerBuilder = consumerBuilder;
    }

    public ConsumerConfig Config => _consumerConfig;

    public IConsumer<TKey, TValue> Create() => _consumerBuilder.Build();
}

