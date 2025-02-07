// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventProcessorClientComponent()
    : EventHubsComponent<AzureMessagingEventHubsProcessorSettings, EventProcessorClient, EventProcessorClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(
        IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> clientBuilder,
        IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsProcessorSettings settings,
        IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions> AddClient(
        AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingEventHubsProcessorSettings settings,
        string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.AddClient<EventProcessorClient, EventProcessorClientOptions>(
            (options, cred, provider) =>
            {
                // ensure that the connection string or namespace+eventhubname is provided 
                EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

                options.Identifier ??= GenerateClientIdentifier(settings.EventHubName, settings.ConsumerGroup);

                var containerClient = GetBlobContainerClient(settings, provider, configurationSectionName);

                var consumerGroup = settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName;

                if (string.IsNullOrEmpty(settings.ConnectionString))
                {
                    return new EventProcessorClient(containerClient,
                        consumerGroup,
                        settings.FullyQualifiedNamespace,
                        settings.EventHubName, cred, options);
                }

                if (string.IsNullOrEmpty(settings.EventHubName))
                {
                    return new EventProcessorClient(containerClient,
                        consumerGroup,
                        settings.ConnectionString, options);
                }

                return new EventProcessorClient(containerClient,
                    consumerGroup,
                    settings.ConnectionString,
                    settings.EventHubName, options);

            });
    }

    private static BlobContainerClient GetBlobContainerClient(
        AzureMessagingEventHubsProcessorSettings settings, IServiceProvider provider, string configurationSectionName)
    {
        // look for keyed client if one is configured. Otherwise, get an unkeyed BlobServiceClient
        var blobClient = !string.IsNullOrEmpty(settings.BlobClientServiceKey) ?
            provider.GetKeyedService<BlobServiceClient>(settings.BlobClientServiceKey) :
            provider.GetService<BlobServiceClient>();

        if (blobClient is null)
        {
            throw new InvalidOperationException(
                $"An EventProcessorClient could not be configured. Ensure a valid 'BlobServiceClient' is available in the ServiceProvider or " +
                $"provide the service key of the 'BlobServiceClient' in " +
                $"the '{configurationSectionName}:BlobClientServiceKey' configuration section, or use the settings callback to configure it in code.");
        }

        // consumer group and blob container names have similar constraints (alphanumeric, hyphen) but we should sanitize nonetheless
        var consumerGroup = (string.IsNullOrWhiteSpace(settings.ConsumerGroup)) ? "default" : settings.ConsumerGroup;

        // Only attempt to create a container if it was NOT found in the connection string
        // this is always the case for an Aspire mounted blob resource, but a dev could provide a blob
        // connection string themselves that includes a container name in the Uri already; in this case
        // we assume it already exists and avoid the extra permission demand. The applies to any container
        // name specified in the settings.
        bool shouldTryCreateIfNotExists = false;

        // Do we have a container name provided in the settings?
        if (string.IsNullOrWhiteSpace(settings.BlobContainerName))
        {
            // If not, we'll create a container name based on the namespace, event hub name and consumer group
            var ns = GetNamespaceFromSettings(settings);

            settings.BlobContainerName = $"{ns}-{settings.EventHubName}-{consumerGroup}";
            shouldTryCreateIfNotExists = true;
        }

        var containerClient = blobClient.GetBlobContainerClient(settings.BlobContainerName);

        if (shouldTryCreateIfNotExists)
        {
            try
            {
                containerClient.CreateIfNotExists();
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException(
                    $"The configured container name of '{settings.BlobContainerName}' does not exist, " +
                    "so an attempt was made to create it automatically and this operation failed. Please ensure the container " +
                    "exists and is specified in the connection string, or if you have provided a BlobContainerName in settings, please " +
                    "ensure it exists. If you don't supply a container name, Aspire will attempt to create one with the name 'namespace-hub-consumergroup'.",
                    ex);
            }
        }

        return containerClient;
    }
}
