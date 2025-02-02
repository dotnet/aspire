// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Producer;

internal sealed class ContinuousProducerWorker(IProducer<Null, string> producer, ILogger<ContinuousProducerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        long i = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var message = new Message<Null, string> { Value = $"Hello, World! {i}" };
            producer.Produce("topic", message);
            logger.LogInformation($"{producer.Name} sent message '{message.Value}'");
            i++;
        }
    }
}
