// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventHubConsumerClientComponent : EventHubsComponent<AzureMessagingEventHubsConsumerSettings, EventHubConsumerClient, EventHubConsumerClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsConsumerSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override IAzureClientBuilder<EventHubConsumerClient, EventHubConsumerClientOptions> AddClient(
        AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingEventHubsConsumerSettings settings,
        string connectionName, string configurationSectionName)
    {
        return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<EventHubConsumerClient, EventHubConsumerClientOptions>((options, cred) =>
        {
            EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

            var consumerGroup = settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName;

            // If no connection is provided use TokenCredential
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                return new EventHubConsumerClient(consumerGroup, settings.FullyQualifiedNamespace, settings.EventHubName, cred, options);
            }
            else
            {
                // If no specific EventHubName is provided, it has to be in the connection string
                if (string.IsNullOrEmpty(settings.EventHubName))
                {
                    return new EventHubConsumerClient(consumerGroup, settings.ConnectionString, options);
                }
                else
                {
                    return new EventHubConsumerClient(consumerGroup, settings.ConnectionString, settings.EventHubName, options);
                }
            }

        }, requiresCredential: false);
    }
}
