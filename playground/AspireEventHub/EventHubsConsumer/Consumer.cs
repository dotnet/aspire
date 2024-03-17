using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHubsConsumer;

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
