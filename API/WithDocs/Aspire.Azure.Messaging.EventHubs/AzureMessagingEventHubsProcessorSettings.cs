// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Represents additional settings for configuring a <see cref="T:Azure.Messaging.EventHubs.EventProcessorClient" />.
/// </summary>
public sealed class AzureMessagingEventHubsProcessorSettings : AzureMessagingEventHubsConsumerBaseSettings
{
    /// <summary>
    /// Gets or sets the connection name used to obtain a connection string for an Azure BlobContainerClient. This is required when the Event Processor is used.
    /// </summary>
    /// <remarks>Applies only to <see cref="T:Azure.Messaging.EventHubs.EventProcessorClient" /></remarks>
    public string? BlobClientConnectionName { get; set; }

    /// <summary>
    /// Get or sets the name of the blob container used to store the checkpoint data. If this container does not exist, Aspire will attempt to create it.
    /// If this is not provided, Aspire will attempt to automatically create a container with a name based on the Namespace, Event Hub name and Consumer Group.
    /// If a container is provided in the connection string, it will override this value and the container will be assumed to exist.
    /// </summary>
    /// <remarks>Applies only to <see cref="T:Azure.Messaging.EventHubs.EventProcessorClient" /></remarks>
    public string? BlobContainerName { get; set; }

    public AzureMessagingEventHubsProcessorSettings();
}
