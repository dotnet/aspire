// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Messaging.EventHubs;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class PartitionReceiverClientComponent()
    : EventHubsComponent<AzureMessagingEventHubsPartitionReceiverSettings, PartitionReceiver, PartitionReceiverOptions>
{
    // cannot be in base class as source generator chokes on generic placeholders
    protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions> clientBuilder, IConfiguration configuration)
    {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
        clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
    }

    protected override void BindSettingsToConfiguration(AzureMessagingEventHubsPartitionReceiverSettings settings, IConfiguration config)
    {
        config.Bind(settings);
    }

    protected override IAzureClientBuilder<PartitionReceiver, PartitionReceiverOptions> AddClient(
        AzureClientFactoryBuilder azureFactoryBuilder, AzureMessagingEventHubsPartitionReceiverSettings settings,
        string connectionName, string configurationSectionName)
    {
        return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<PartitionReceiver, PartitionReceiverOptions>((options, cred) =>
        {
            // ensure that the connection string or namespace+eventhubname is provided
            EnsureConnectionStringOrNamespaceProvided(settings, connectionName, configurationSectionName);

            if (string.IsNullOrEmpty(settings.PartitionId))
            {
                throw new InvalidOperationException(
                    $"A PartitionReceiver could not be configured. Ensure a valid PartitionId was provided in the '{configurationSectionName}' configuration section.");
            }

            options.Identifier ??= GenerateClientIdentifier(settings.EventHubName, settings.ConsumerGroup);

            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                return new PartitionReceiver(
                    settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName,
                    settings.PartitionId,
                    settings.EventPosition,
                    settings.FullyQualifiedNamespace,
                    settings.EventHubName,
                    cred, options);
            }

            if (string.IsNullOrEmpty(settings.EventHubName))
            {
                return new PartitionReceiver(
                    settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName,
                    settings.PartitionId,
                    settings.EventPosition,
                    settings.ConnectionString,
                    options);
            }

            return new PartitionReceiver(
                settings.ConsumerGroup ?? EventHubConsumerClient.DefaultConsumerGroupName,
                settings.PartitionId,
                settings.EventPosition,
                settings.ConnectionString,
                settings.EventHubName,
                options);

        }, requiresCredential: false);
    }
}
