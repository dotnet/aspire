// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.EventHubs;

public abstract class AzureMessagingEventHubsBaseSettings : IConnectionStringSettings
{
    public string? ConnectionString { get; set; }
    public string? Namespace { get; set; }
    public string? EventHubName { get; set; }
    public TokenCredential? Credential { get; set; }
    public bool Tracing { get; set; }
    protected AzureMessagingEventHubsBaseSettings();
}
