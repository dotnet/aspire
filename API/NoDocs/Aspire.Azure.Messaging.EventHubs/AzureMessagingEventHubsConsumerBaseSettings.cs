// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;

namespace Aspire.Azure.Messaging.EventHubs;

public abstract class AzureMessagingEventHubsConsumerBaseSettings : AzureMessagingEventHubsBaseSettings
{
    public string? ConsumerGroup { get; set; }
    protected AzureMessagingEventHubsConsumerBaseSettings();
}
