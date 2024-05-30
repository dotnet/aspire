// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Producer;

internal sealed class IntermittentProducerWorker(IProducer<string, string> producer, ILogger<IntermittentProducerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            for (int j = 0; j < 1000; j++, i++)
            {
                var message = new Message<string, string> { Value = $"Hello, World! {i}" };
                producer.Produce("topic", message);
            }

            producer.Flush(stoppingToken);

            logger.LogInformation($"{producer.Name} sent 1000 messages, waiting 10 s");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
