// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Aspire.Confluent.Kafka.Tests;

internal static class ProducerExtensions
{
    public static async Task FlushAsync<TKey, TValue>(this IProducer<TKey, TValue> producer)
    {
        while (producer.Flush(TimeSpan.FromMilliseconds(100)) != 0)
        {
            await Task.Delay(100);
        }
    }
}
