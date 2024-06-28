// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class EventHubBufferedProducerClientComponent : EventHubsComponent<AzureMessagingEventHubsBufferedProducerSettings, EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>
{
    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsBufferedProducerSettings settings,
        IConfiguration configuration)
    {
        configuration.Bind(settings);
    }

    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override IAzureClientBuilder<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions> AddClient(AzureClientFactoryBuilder azureFactoryBuilder,
        AzureMessagingEventHubsBufferedProducerSettings settings, string connectionName, string configurationSectionName)
    {
        return ((IAzureClientFactoryBuilderWithCredential) azureFactoryBuilder)
            .RegisterClientFactory<EventHubBufferedProducerClient, EventHubBufferedProducerClientOptions>(
                (options, cred) =>
                {
                    EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

                    return !string.IsNullOrEmpty(settings.ConnectionString)
                        ? new EventHubBufferedProducerClient(settings.ConnectionString, options)
                        : new EventHubBufferedProducerClient(settings.Namespace, settings.EventHubName, cred, options);
                }, requiresCredential: false);
    }
}

