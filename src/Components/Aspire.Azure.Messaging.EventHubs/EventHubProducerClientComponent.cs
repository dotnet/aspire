// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventHubProducerClientComponent : EventHubsComponent<EventHubProducerClient, EventHubProducerClientOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureMessagingEventHubsSettings settings,
        string connectionName, string configurationSectionName)
    {
        return azureFactoryBuilder.RegisterClientFactory<EventHubProducerClient, EventHubProducerClientOptions>((options, cred) =>
        {
            var connectionString = settings.ConnectionString;
            if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(settings.Namespace))
            {
                throw new InvalidOperationException($"A EventHubProducerClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'Namespace' in the '{configurationSectionName}' configuration section.");
            }

            return !string.IsNullOrEmpty(connectionString) ?
                new EventHubProducerClient(connectionString, options) :
                new EventHubProducerClient(settings.Namespace, settings.EventHubName, cred, options);
        }, requiresCredential: false);
    }
}
