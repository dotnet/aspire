// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Represents additional settings for configuring an Event Hubs client that may accept a ConsumerGroup
/// </summary>
public abstract class AzureMessagingEventHubsConsumerBaseSettings : AzureMessagingEventHubsBaseSettings
{
    /// <summary>
    /// Gets or sets the name of the consumer group.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    protected AzureMessagingEventHubsConsumerBaseSettings();
}
