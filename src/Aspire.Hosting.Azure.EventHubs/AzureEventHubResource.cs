// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Event Hub.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureEventHubResource : Resource, IResourceWithParent<AzureEventHubsResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEventHubResource"/> class.
    /// </summary>
    public AzureEventHubResource(string name, string hubName, AzureEventHubsResource parent) : base(name)
    {
        HubName = hubName;
        Parent = parent;
    }

    /// <summary>
    /// The event hub name.
    /// </summary>
    public string HubName { get; set; }

    /// <summary>
    /// Number of partitions created for the Event Hub, allowed values are from
    /// 1 to 32 partitions.
    /// </summary>
    public long? PartitionCount { get; set; }

    /// <summary>
    /// Gets the parent Azure Event Hubs resource.
    /// </summary>
    public AzureEventHubsResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Event Hub.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    /// <summary>
    /// The consumer groups for this hub.
    /// </summary>
    internal List<AzureEventHubConsumerGroupResource> ConsumerGroups { get; } = [];

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.EventHubs.EventHub"/> instance.</returns>
    internal global::Azure.Provisioning.EventHubs.EventHub ToProvisioningEntity()
    {
        var hub = new global::Azure.Provisioning.EventHubs.EventHub(Infrastructure.NormalizeBicepIdentifier(Name));

        hub.Name = HubName;

        if (PartitionCount.HasValue)
        {
            hub.PartitionCount = PartitionCount.Value;
        }

        return hub;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var hub = this;

        writer.WriteString(nameof(Name), hub.HubName);

        if (hub.PartitionCount.HasValue)
        {
            writer.WriteNumber(nameof(PartitionCount), hub.PartitionCount.Value);
        }
        else
        {
            // Value is required. We don't assign it by default in case
            // we need to detect if the value was never set or if the defaults
            // in Azure.Provisioning change.

            writer.WriteNumber(nameof(PartitionCount), 1);
        }

#pragma warning disable CA1507 // Use nameof to express symbol names: there is no direct link between the property name and the JSON representation
        writer.WriteStartArray("ConsumerGroups");

        // The default consumer group ('$default') is automatically created by the
        // emulator. We don't need to create it explicitly.

        if (hub.ConsumerGroups.Count >= 0)
        {
            foreach (var consumerGroup in hub.ConsumerGroups)
            {
                writer.WriteStartObject();
                consumerGroup.WriteJsonObjectProperties(writer);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }
#pragma warning restore CA1507 // Use nameof to express symbol names

}
