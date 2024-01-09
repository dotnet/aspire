// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class KafkaExtensions
{
    public static void MapKafkaApi(this WebApplication app)
    {
        app.MapGet("/kafka/produce/{topic}", async (IProducer<string, string> producer, string topic, ILogger<IProducer<string, string>> logger) =>
        {
            int i = 0;
            for (i = 0; i < 100; i++)
            {
                logger.LogInformation("Producing message {i}", i);
                await producer.ProduceAsync(topic, new Message<string, string> { Key = "test-key", Value = "test-value" });
                logger.LogInformation("Produced message {i}", i);
            }

            return Results.Ok(i);
        });

        app.MapGet("/kafka/consume/{topic}", (IConsumer<string, string> consumer, string topic, ILogger<IConsumer<string, string>> logger) =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            consumer.Subscribe(topic);

            int i = 0;
            for (i = 0; i < 100; i++)
            {
                logger.LogInformation("Consuming message {i}", i);
                consumer.Consume(cts.Token);
                logger.LogInformation("Consumed message {i}", i);
            }

            return Results.Ok(i);
        });
    }
}
