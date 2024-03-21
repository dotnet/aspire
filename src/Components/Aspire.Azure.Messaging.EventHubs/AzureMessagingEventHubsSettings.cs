// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Service Bus.
/// </summary>
public sealed class AzureMessagingEventHubsSettings : IConnectionStringSettings
{
    private bool? _tracing;

    /// <summary>
    /// Gets or sets the connection string used to connect to the Service Bus namespace. 
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Namespace"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace. 
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the Event Hub.
    /// </summary>
    public string? EventHubName { get; set; }

    /// <summary>
    /// Gets or sets the name of the consumer group.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets the connection name used to obtain a connection string for an Azure BlobContainerClient. This is required when the Event Processor is used.
    /// </summary>
    /// <remarks>Applies only to <see cref="EventProcessorClient"/></remarks>
    public string? BlobClientConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the partition identifier.
    /// </summary>
    /// <remarks>Applies only to <see cref="PartitionReceiver"/></remarks>
    public string? PartitionId { get; set; }

    /// <summary>
    /// Gets or sets the event position to start from in the bound partition. Defaults to <see cref="EventPosition.Latest" />.
    /// </summary>
    /// <remarks>Applies only to <see cref="PartitionReceiver"/></remarks>
    public EventPosition EventPosition { get; set; } = EventPosition.Earliest;

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Service Bus namespace.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <remarks>
    /// ServiceBus ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing
    {
        get { return _tracing ??= GetTracingDefaultValue(); }
        set { _tracing = value; }
    }

    // default Tracing to true if the experimental switch is set
    // TODO: remove this when ActivitySource support is no longer experimental
    private static bool GetTracingDefaultValue()
    {
        if (AppContext.TryGetSwitch("Azure.Experimental.EnableActivitySource", out var enabled))
        {
            return enabled;
        }

        var envVar = Environment.GetEnvironmentVariable("AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE");
        if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
        {
            return true;
        }

        return false;
    }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            // a service bus namespace can't contain ';'. if it is found assume it is a connection string
            if (!connectionString.Contains(';'))
            {
                Namespace = connectionString;
            }
            else
            {
                ConnectionString = connectionString;
            }
        }
    }
}
