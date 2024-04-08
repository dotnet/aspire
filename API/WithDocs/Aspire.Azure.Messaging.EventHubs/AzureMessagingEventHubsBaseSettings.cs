// Assembly 'Aspire.Azure.Messaging.EventHubs'

using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Represents additional shared settings for configuring an Event Hubs client.
/// </summary>
public abstract class AzureMessagingEventHubsBaseSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the Event Hubs namespace. 
    /// </summary>
    /// <remarks>
    /// If <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.ConnectionString" /> is set, it overrides <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Namespace" /> and <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Credential" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Event Hubs namespace. 
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="P:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBaseSettings.Credential" /> to establish the connection.
    /// </remarks>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the Event Hub.
    /// </summary>
    public string? EventHubName { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Event Hubs namespace.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <remarks>
    /// Event Hubs ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="T:System.AppContext" /> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing { get; set; }

    protected AzureMessagingEventHubsBaseSettings();
}
