using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace EventHubsConsumer;

/// <summary>
///   Demonstrate consuming events from an Event Hub using the <see cref="EventHubConsumerClient" />.
/// </summary>
/// <remarks>
///   This method is not recommended for production use; the <see cref="EventProcessorClient"/> should be used for reading events from all partitions in a
///   production scenario, as it offers a much more robust experience with higher throughput.
///
///   It is important to note that this method does not guarantee fairness amongst the partitions during iteration; each of the partitions compete to publish
///   events to be read by the enumerator. Depending on service communication, there may be a clustering of events per partition and/or there may be a noticeable
///   bias for a given partition or subset of partitions.
///
///   Each reader of events is presented with an independent iterator; if there are multiple readers, each receive their own copy of an event to
///   process, rather than competing for them.
/// </remarks>
internal sealed class Consumer(EventHubConsumerClient client, ILogger<Consumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        void LogString(string message) => logger.Log(LogLevel.Information, 0, message, null, (s, _) => s);

        await foreach (var partition in client.ReadEventsAsync(stoppingToken))
        {
            LogString(partition.Data.EventBody.ToString());
        }
    }
}
