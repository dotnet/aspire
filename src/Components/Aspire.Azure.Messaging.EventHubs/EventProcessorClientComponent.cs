// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
//using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventProcessorClientComponent(IConfiguration builderConfiguration)
    : EventHubsComponent<EventProcessorClient, EventProcessorClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureMessagingEventHubsSettings settings,
        string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.RegisterClientFactory<EventProcessorClient, EventProcessorClientOptions>(
            (options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.Namespace))
                {
                    throw new InvalidOperationException(
                        $"A EventHubProducerClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
                }

                if (string.IsNullOrEmpty(settings.BlobClientConnectionName))
                {
                    // throw an invalid operation exception if the blob client connection string is not provided
                    throw new InvalidOperationException(
                                               $"A EventProcessorClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'BlobClientConnectionName' in the '{configurationSectionName}' configuration section.");
                }

                // todo: add more settings and clientoptions validation; also we should use a deterministic ID for the processor

                var blobConnectionString =
                    builderConfiguration.GetConnectionString(settings.BlobClientConnectionName);

                Uri blobUri = new Uri($"{blobConnectionString}/checkpoints"); // fixme: this should be a compound name including the consumer group name ideally
                var blobClient = new BlobContainerClient(blobUri, cred);
                blobClient.CreateIfNotExists(); // this works, but the processor client fails to create blobs later?

                var processor = !string.IsNullOrEmpty(connectionString)
                    ? new EventProcessorClient(blobClient,
                        settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName, connectionString)
                    : new EventProcessorClient(blobClient,
                        settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName, settings.Namespace,
                        settings.EventHubName, cred, options);

                // ...

                return processor;

            }, requiresCredential: false);

    }
}
