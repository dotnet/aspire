// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;
using Azure.Messaging.EventHubs.Consumer;

namespace Aspire.Azure.Messaging.EventHubs;

public sealed class AzureMessagingEventHubsPartitionReceiverSettings : AzureMessagingEventHubsConsumerBaseSettings
{
    public string? PartitionId { get; set; }
    public EventPosition EventPosition { get; set; }
    public AzureMessagingEventHubsPartitionReceiverSettings();
}
