// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using static Aspire.Confluent.Kafka.ConfluentKafkaMetrics;

namespace Aspire.Confluent.Kafka;

internal sealed class MetricsService(MetricsChannel channel, ConfluentKafkaMetrics metrics) : BackgroundService
{
    private readonly MetricsChannel _channel = channel;
    private readonly Dictionary<string, Statistics> _state = new();
    private readonly ConfluentKafkaMetrics _metrics = metrics;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var json))
            {
                Statistics? statistics;
                try
                {
                     statistics = JsonSerializer.Deserialize(json, StatisticsJsonSerializerContext.Default.Statistics);
                }
                catch
                {
                    return;
                }

                if (statistics == null || statistics.Name == null)
                {
                    return;
                }

                TagList tags = new()
                {
                    { Tags.ClientId, statistics.ClientId },
                    { Tags.Name, statistics.Name }
                };

                _metrics.ReplyQueueMeasurements.Enqueue(new Measurement<long>(statistics.ReplyQueue, tags));
                _metrics.MessageCountMeasurements.Enqueue(new Measurement<long>(statistics.MessageCount, tags));
                _metrics.MessageSizeMeasurements.Enqueue(new Measurement<long>(statistics.MessageSize, tags));

                tags.Add(new KeyValuePair<string, object?> (Tags.Type, statistics.Type));

                if (_state.TryGetValue(statistics.Name, out var previous))
                {
                    _metrics.Tx.Add(statistics.Tx - previous.Tx, tags);
                    _metrics.TxBytes.Add(statistics.TxBytes - previous.TxBytes, tags);
                    _metrics.TxMessages.Add(statistics.TxMessages - previous.TxMessages, tags);
                    _metrics.TxMessageBytes.Add(statistics.TxMessageBytes - previous.TxMessageBytes, tags);
                    _metrics.Rx.Add(statistics.Rx - previous.Rx, tags);
                    _metrics.RxBytes.Add(statistics.RxBytes - previous.RxBytes, tags);
                    _metrics.RxMessages.Add(statistics.RxMessages - previous.RxMessages, tags);
                    _metrics.RxMessageBytes.Add(statistics.RxMessageBytes - previous.RxMessageBytes, tags);
                }
                else
                {
                    _metrics.Tx.Add(statistics.Tx, tags);
                    _metrics.TxBytes.Add(statistics.TxBytes, tags);
                    _metrics.TxMessages.Add(statistics.TxMessages, tags);
                    _metrics.TxMessageBytes.Add(statistics.TxMessageBytes, tags);
                    _metrics.Rx.Add(statistics.Rx, tags);
                    _metrics.RxBytes.Add(statistics.RxBytes, tags);
                    _metrics.RxMessages.Add(statistics.RxMessages, tags);
                    _metrics.RxMessageBytes.Add(statistics.RxMessageBytes, tags);
                }

                _state[statistics.Name] = statistics;
            }
        }
    }
}
