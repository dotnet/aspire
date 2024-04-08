// Assembly 'Aspire.Azure.Messaging.ServiceBus'

using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Service Bus.
/// </summary>
public sealed class AzureMessagingServiceBusSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the Service Bus namespace. 
    /// </summary>
    /// <remarks>
    /// If <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.ConnectionString" /> is set, it overrides <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.Namespace" /> and <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.Credential" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace. 
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="P:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.Credential" /> to establish the connection.
    /// </remarks>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Service Bus namespace.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Name of the queue used by the health check. Mandatory to get health checks enabled.
    /// </summary>
    public string? HealthCheckQueueName { get; set; }

    /// <summary>
    /// Name of the topic used by the health check. Mandatory to get health checks enabled.
    /// </summary>
    public string? HealthCheckTopicName { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <remarks>
    /// ServiceBus ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="T:System.AppContext" /> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing { get; set; }

    public AzureMessagingServiceBusSettings();
}
