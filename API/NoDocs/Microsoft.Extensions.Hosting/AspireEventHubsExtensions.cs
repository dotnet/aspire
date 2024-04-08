// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;

namespace Microsoft.Extensions.Hosting;

public static class AspireEventHubsExtensions
{
    public static void AddAzureEventProcessorClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null, Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureEventProcessorClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsProcessorSettings>? configureSettings = null, Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>>? configureClientBuilder = null);
    public static void AddAzurePartitionReceiverClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null, Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzurePartitionReceiverClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsPartitionReceiverSettings>? configureSettings = null, Action<IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions>>? configureClientBuilder = null);
    public static void AddAzureEventHubProducerClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureEventHubProducerClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsProducerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions>>? configureClientBuilder = null);
    public static void AddAzureEventHubConsumerClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureEventHubConsumerClient(this IHostApplicationBuilder builder, string name, Action<AzureMessagingEventHubsConsumerSettings>? configureSettings = null, Action<IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions>>? configureClientBuilder = null);
}
