// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace EventHubsConsumer;

internal sealed class Processor(EventProcessorClient client, ILogger<Consumer> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting processor...");

        client.ProcessEventAsync += ClientOnProcessEventAsync;
        
        Task ClientOnProcessEventAsync(ProcessEventArgs arg)
        {
            logger.LogInformation(arg.Data.EventBody.ToString());
            return Task.CompletedTask;
        }

        await client.StartProcessingAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Entering execute - 30 second run");
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping processor...");
        await client.StopProcessingAsync(cancellationToken);
    }
}
