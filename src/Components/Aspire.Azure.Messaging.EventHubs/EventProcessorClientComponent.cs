// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventProcessorClientComponent(IConfiguration builderConfiguration)
    : EventHubsComponent<AzureMessagingEventHubsProcessorSettings, EventProcessorClient, EventProcessorClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsProcessorSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureMessagingEventHubsProcessorSettings settings,
        string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.RegisterClientFactory<EventProcessorClient, EventProcessorClientOptions>(
            (options, cred) =>
            {
                EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

                options.Identifier ??= GenerateClientIdentifier(settings.EventHubName, settings.ConsumerGroup);

                var blobClient = GetBlobContainerClient(settings, cred, configurationSectionName);

                var processor = !string.IsNullOrEmpty(settings.ConnectionString)
                    ? new EventProcessorClient(blobClient,
                        settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName, settings.ConnectionString)
                    : new EventProcessorClient(blobClient,
                        settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName, settings.FullyQualifiedNamespace,
                        settings.EventHubName, cred, options);

                return processor;

            }, requiresCredential: false);
    }

    private BlobContainerClient GetBlobContainerClient(
        AzureMessagingEventHubsProcessorSettings settings, TokenCredential cred, string configurationSectionName)
    {
        if (string.IsNullOrEmpty(settings.BlobClientConnectionName))
        {
            // throw an invalid operation exception if the blob client connection name is not provided
            throw new InvalidOperationException(
                $"A EventProcessorClient could not be configured. Ensure a valid blob connection name was provided in " +
                $"the '{configurationSectionName}:BlobClientConnectionName' configuration section.");
        }

        var blobConnectionString =
            builderConfiguration.GetConnectionString(
                settings.BlobClientConnectionName) ??
                throw new InvalidOperationException(
                    "An EventProcessorClient could not be configured. " +
                    $"There is no connection string in Configuration with the name {settings.BlobClientConnectionName}. " +
                    "Ensure you have configured a connection for a Azure Blob Storage Account.");

        // FIXME: ideally this should be pulled from services; but thar be dragons.
        // There is no reliable way to get the blob service client from the services collection
        var blobUriBuilder = new BlobUriBuilder(new Uri(blobConnectionString!));

        // consumer group and blob container names have similar constraints (alphanumeric, hyphen) but we should sanitize nonetheless
        var consumerGroup = (string.IsNullOrWhiteSpace(settings.ConsumerGroup)) ? "default" : settings.ConsumerGroup;

        // Only attempt to create a container if it was NOT found in the connection string
        // this is always the case for an Aspire mounted blob resource, but a dev could provide a blob
        // connection string themselves that includes a container name in the Uri already; in this case
        // we assume it already exists and avoid the extra permission demand. The applies to any container
        // name specified in the settings.
        if (blobUriBuilder.BlobContainerName == string.Empty)
        {
            var ns = GetNamespaceFromSettings(settings);

            // Do we have a container name provided in the settings?
            if (string.IsNullOrWhiteSpace(settings.BlobContainerName))
            {
                // If not, we'll create a container name based on the namespace, event hub name and consumer group
                blobUriBuilder.BlobContainerName = $"{ns}-{settings.EventHubName}-{consumerGroup}";
            }
            else
            {
                // If a container name is provided, we'll use that
                blobUriBuilder.BlobContainerName = settings.BlobContainerName;
            }

            var blobClient = new BlobContainerClient(blobUriBuilder.ToUri(), cred);

            try
            {
                blobClient.CreateIfNotExists();

                return blobClient;
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException(
                    $"The configured container name of '{blobUriBuilder.BlobContainerName}' does not exist, " +
                    "so an attempt was made to create it automatically and this operation failed. Please ensure the container " +
                    "exists and is specified in the connection string, or if you have provided a BlobContainerName in settings, please " +
                    "ensure it exists. If you don't supply a container name, Aspire will attempt to create one with the name 'namespace-hub-consumergroup'.",
                    ex);
            }
        }

        return new BlobContainerClient(blobUriBuilder.ToUri(), cred);
    }
}
