// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;

namespace EventHubsConsumer;

/// <summary>
///   Demonstrates how to use the <see cref="EventProcessorClient"/> to process events from an Azure Event Hub.
/// </summary>
/// <remarks>
///   The EventProcessorClient in Azure Event Hubs is a powerful tool that allows for the processing of events from multiple partitions concurrently.
///   It’s crucial to spread the reading of partitions across multiple nodes for load balancing and to maximize throughput.
///   This is achieved by assigning each partition to a specific node, ensuring that the workload is evenly distributed.
///   The EventProcessorClient is configured with a <see cref="BlobContainerClient"/> to store checkpoint and ownership data.
///   Use <see cref="EventProcessorClientOptions"/> to specify options such as the maximum wait time and prefetch count.
///   Proper configuration and usage of the EventProcessorClient can significantly improve the efficiency and performance of your Azure Event Hubs
///   applications. Remember to handle exceptions and monitor the health of your nodes to ensure smooth operation.
/// </remarks>
/// <example>
///   See samples at https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/samples
/// </example>
/// <param name="client"></param>
/// <param name="logger"></param>
internal sealed class Processor(EventProcessorClient client, ILogger<Consumer> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting processor...");

        client.ProcessEventAsync += arg =>
        {
            logger.LogInformation(arg.Data.EventBody.ToString());
            return Task.CompletedTask;
        };

        client.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Error processing message: {Error}", args.Exception.Message);
            return Task.CompletedTask;
        };

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
