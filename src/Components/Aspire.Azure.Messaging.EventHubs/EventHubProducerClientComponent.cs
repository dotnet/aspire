// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventHubProducerClientComponent : EventHubsComponent<AzureMessagingEventHubsProducerSettings, EventHubProducerClient, EventHubProducerClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsProducerSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }
    
    protected override IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddClient(
        AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingEventHubsProducerSettings settings,
        string connectionName, string configurationSectionName)
    {
        return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<EventHubProducerClient, EventHubProducerClientOptions>((options, cred) =>
        {
            // ensure that the connection string or namespace+eventhubname is provided
            EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

            // If no connection is provided use TokenCredential
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                return new EventHubProducerClient(settings.FullyQualifiedNamespace, settings.EventHubName, cred, options);
            }

            // If no specific EventHubName is provided, it has to be in the connection string
            if (string.IsNullOrEmpty(settings.EventHubName))
            {
                return new EventHubProducerClient(settings.ConnectionString, options);
            }

            return new EventHubProducerClient(settings.ConnectionString, settings.EventHubName, options);

        }, requiresCredential: false);
    }
}
