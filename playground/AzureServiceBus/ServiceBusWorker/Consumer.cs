using System.Text;
using Azure.Messaging.ServiceBus;

namespace ServiceBusWorker;

internal sealed class Consumer(ServiceBusClient client, ILogger<Consumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var processor = client.CreateProcessor("queue1", new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = true,
            MaxConcurrentCalls = 1, // Process one message at a time
        });

        processor.ProcessMessageAsync += MessageHandler;

        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private Task MessageHandler(ProcessMessageEventArgs args)
    {
        // Process the message
        logger.LogInformation("Received message: {Message}", Encoding.UTF8.GetString(args.Message.Body));

        return Task.CompletedTask;
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping consumer...");
        return Task.CompletedTask;
    }
}
