// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;

using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;

namespace Aspire.Azure.Messaging.EventHubs;

/// <summary>
/// Represents additional shared settings for configuring an Event Hubs client.
/// </summary>
public abstract class AzureMessagingEventHubsSettings : IConnectionStringSettings
{
    private bool? _disableTracing;

    internal AzureMessagingEventHubsSettings() { }

    /// <summary>
    /// Gets or sets the connection string used to connect to the Event Hubs namespace.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="FullyQualifiedNamespace"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Event Hubs namespace.
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the Event Hub.
    /// </summary>
    public string? EventHubName { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Event Hubs namespace.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Azure Event Hubs health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <remarks>
    /// Event Hubs ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    /// <value>  
    /// The default value is <see langword="false"/>.  
    /// </value>
    public bool DisableTracing
    {
        get { return _disableTracing ??= !GetTracingDefaultValue(); }
        set { _disableTracing = value; }
    }

    // Defaults DisableTracing to false if the experimental switch is set
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

    /// <summary>
    /// Extracts properties from the connection string.
    /// </summary>
    /// <remarks>
    /// The connection string can be in the following formats:
    /// A valid FullyQualifiedNamespace
    /// Endpoint={valid FullyQualifiedNamespace} - optionally with [;EntityPath={hubName}[;ConsumerGroup={groupName}]]
    /// {valid EventHub ConnectionString} - optionally with [;ConsumerGroup={groupName}]
    /// </remarks>
    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            // an event hubs namespace can't contain ';'. if it is found assume it is a connection string
            if (!connectionString.Contains(';'))
            {
                FullyQualifiedNamespace = connectionString;
                return;
            }

            var connectionBuilder = new StableConnectionStringBuilder(connectionString);

            // Note: Strip out the ConsumerGroup and EntityPath from the connection string in order
            // to tell if we are left with just Endpoint.

            if (connectionBuilder.TryGetValue("ConsumerGroup", out var group))
            {
                SetConsumerGroup(group.ToString());

                connectionBuilder.Remove("ConsumerGroup");
            }

            if (connectionBuilder.TryGetValue("EntityPath", out var entityPath))
            {
                EventHubName = entityPath!.ToString();

                connectionBuilder.Remove("EntityPath");
            }

            if (connectionBuilder.Count() == 1 &&
                connectionBuilder.TryGetValue("Endpoint", out var endpoint))
            {
                // if all that's left is Endpoint, it is a fully qualified namespace
                FullyQualifiedNamespace = endpoint.ToString();
                return;
            }

            // if we got here, it's a full connection string
            // use the original connection string since connectionBuilder was modified above.
            // Extra keys, like ConsumerGroup, are ignored by the EventHub client constructors.
            ConnectionString = connectionString;
        }
    }

    internal virtual void SetConsumerGroup(string? consumerGroup) { }
}

/// <summary>
/// Represents additional settings for configuring a <see cref="EventHubProducerClient"/>.
/// </summary>
public sealed class AzureMessagingEventHubsProducerSettings : AzureMessagingEventHubsSettings { }

/// <summary>
/// Represents additional settings for configuring a <see cref="EventHubBufferedProducerClient"/>.
/// </summary>
public sealed class AzureMessagingEventHubsBufferedProducerSettings : AzureMessagingEventHubsSettings { }

/// <summary>
/// Represents additional settings for configuring a <see cref="EventHubConsumerClient"/>.
/// </summary>
public sealed class AzureMessagingEventHubsConsumerSettings : AzureMessagingEventHubsSettings
{
    /// <summary>
    /// Gets or sets the name of the consumer group.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    internal override void SetConsumerGroup(string? consumerGroup)
    {
        ConsumerGroup = consumerGroup;
    }
}

/// <summary>
/// Represents additional settings for configuring a <see cref="EventProcessorClient"/>.
/// </summary>
public sealed class AzureMessagingEventHubsProcessorSettings : AzureMessagingEventHubsSettings
{
    /// <summary>
    /// Gets or sets the name of the consumer group.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets the IServiceProvider service key used to obtain an Azure BlobServiceClient.
    /// </summary>
    /// <remarks>
    /// A BlobServiceClient is required when using the Event Processor. If a BlobClientServiceKey is not configured,
    /// an un-keyed BlobServiceClient will be retrieved from the IServiceProvider. If a BlobServiceClient is not available in
    /// the IServiceProvider, an exception is thrown.
    /// </remarks>
    public string? BlobClientServiceKey { get; set; }

    /// <summary>
    /// Get or sets the name of the blob container used to store the checkpoint data. If this container does not exist, Aspire will attempt to create it.
    /// If this is not provided, Aspire will attempt to automatically create a container with a name based on the Namespace, Event Hub name and Consumer Group.
    /// If a container is provided in the connection string, it will override this value and the container will be assumed to exist.
    /// </summary>
    public string? BlobContainerName { get; set; }

    internal override void SetConsumerGroup(string? consumerGroup)
    {
        ConsumerGroup = consumerGroup;
    }
}

/// <summary>
/// Represents additional settings for configuring a <see cref="PartitionReceiver"/>.
/// </summary>
public sealed class AzureMessagingEventHubsPartitionReceiverSettings : AzureMessagingEventHubsSettings
{
    /// <summary>
    /// Gets or sets the name of the consumer group.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets the partition identifier.
    /// </summary>
    public string? PartitionId { get; set; }

    /// <summary>
    /// Gets or sets the event position to start from in the bound partition. Defaults to <see cref="EventPosition.Earliest" />.
    /// </summary>
    public EventPosition EventPosition { get; set; } = EventPosition.Earliest;

    internal override void SetConsumerGroup(string? consumerGroup)
    {
        ConsumerGroup = consumerGroup;
    }
}

