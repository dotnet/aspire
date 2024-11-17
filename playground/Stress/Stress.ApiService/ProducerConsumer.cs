// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;

namespace Stress.ApiService;

public class Data
{
    public required int Id { get; init; }
    public required Activity? Producer { get; init; }
    public required string Message { get; init; }
}

public class ProducerConsumer
{
    public const string ActivitySourceName = "ProducerConsumer";

    private readonly Channel<Data> _channel = Channel.CreateUnbounded<Data>();

    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    public async Task ProduceAndConsumeAsync(int count)
    {
        var consumerTask = Task.Run(async () =>
        {
            using var appActivity = s_activitySource.StartActivity("ConsumerApp", ActivityKind.Internal);

            await foreach (var item in _channel.Reader.ReadAllAsync())
            {
                var links = new List<ActivityLink>();
                if (item.Producer != null)
                {
                    links.Add(new ActivityLink(new ActivityContext(item.Producer.TraceId, item.Producer.SpanId, ActivityTraceFlags.None)));
                }

                using var activity = s_activitySource.StartActivity($"Consume {item.Id}", ActivityKind.Consumer, parentId: null, links: links);

                await Task.Delay(Random.Shared.Next(10, 50));
            }
        });

        using var appActivity = s_activitySource.StartActivity("ProducerApp", ActivityKind.Internal);

        for (var i = 0; i < count; i++)
        {
            var id = i + 1;

            Data data;
            using (var activity = s_activitySource.StartActivity($"Produce {id}", ActivityKind.Producer))
            {
                await Task.Delay(Random.Shared.Next(10, 50));

                data = new Data
                {
                    Id = id,
                    Producer = activity,
                    Message = $"Message {id}"
                };
            }

            await _channel.Writer.WriteAsync(data);
        }
        _channel.Writer.Complete();

        await consumerTask;
    }
}
