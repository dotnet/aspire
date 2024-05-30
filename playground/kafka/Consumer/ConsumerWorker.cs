// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;

namespace Consumer;

internal sealed class ConsumerWorker(IConsumer<Ignore, string> consumer, ILogger<ConsumerWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long i = 0;
        return Task.Factory.StartNew(async () =>
        {
            consumer.Subscribe("topic");
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? result = default;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(1));
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    await Task.Delay(100);
                    continue;
                }

                i++;
                if (i % 1000 == 0)
                {
                    logger.LogInformation($"Received {i} messages. current offset is '{result!.Offset}'");
                }
            }
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }
}
