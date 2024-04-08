// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;

namespace Aspire.Azure.Messaging.EventHubs;

public sealed class AzureMessagingEventHubsProcessorSettings : AzureMessagingEventHubsConsumerBaseSettings
{
    public string? BlobClientConnectionName { get; set; }
    public string? BlobContainerName { get; set; }
    public AzureMessagingEventHubsProcessorSettings();
}
