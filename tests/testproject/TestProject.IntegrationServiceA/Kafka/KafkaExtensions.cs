// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class KafkaExtensions
{
    public static void MapKafkaApi(this WebApplication app)
    {
        app.MapGet("/kafka/produce/{topic}", async (IProducer<string, string> producer, string topic) =>
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    await producer.ProduceAsync(topic, new Message<string, string> { Key = "test-key", Value = "test-value" });
                }

                return Results.Ok("Success!");
            }
            catch (Exception e)
            {
                return Results.Problem(e.ToString());
            }
        });

        app.MapGet("/kafka/consume/{topic}", (IConsumer<string, string> consumer, string topic) =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                consumer.Subscribe(topic);

                for (int i = 0; i < 100; i++)
                {
                    consumer.Consume(cts.Token);
                }

                return Results.Ok("Success!");
            }
            catch (Exception e)
            {
                return Results.Problem(e.ToString());
            }
        });
    }
}
