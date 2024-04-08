// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;
using Azure.Messaging.EventHubs.Consumer;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Represents additional settings for configuring a <see cref="T:Azure.Messaging.EventHubs.Primitives.PartitionReceiver" />.
/// </summary>
public sealed class AzureMessagingEventHubsPartitionReceiverSettings : AzureMessagingEventHubsConsumerBaseSettings
{
    /// <summary>
    /// Gets or sets the partition identifier.
    /// </summary>
    /// <remarks>Applies only to <see cref="T:Azure.Messaging.EventHubs.Primitives.PartitionReceiver" /></remarks>
    public string? PartitionId { get; set; }

    /// <summary>
    /// Gets or sets the event position to start from in the bound partition. Defaults to <see cref="P:Azure.Messaging.EventHubs.Consumer.EventPosition.Earliest" />.
    /// </summary>
    /// <remarks>Applies only to <see cref="T:Azure.Messaging.EventHubs.Primitives.PartitionReceiver" /></remarks>
    public EventPosition EventPosition { get; set; }

    public AzureMessagingEventHubsPartitionReceiverSettings();
}
