// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Event Hub Consumer Group.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureEventHubConsumerGroupResource : Resource, IResourceWithParent<AzureEventHubResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEventHubConsumerGroupResource"/> class.
    /// </summary>
    public AzureEventHubConsumerGroupResource(string name, string consumerGroupName, AzureEventHubResource parent) : base(name)
    {
        ConsumerGroupName = consumerGroupName;
        Parent = parent;
    }

    /// <summary>
    /// The event hub consumer group name.
    /// </summary>
    public string ConsumerGroupName { get; set; }

    /// <summary>
    /// Gets the parent Azure Event Hub resource.
    /// </summary>
    public AzureEventHubResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Event Hub.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.EventHubs.EventHub"/> instance.</returns>
    internal global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup ToProvisioningEntity()
    {
        var consumerGroup = new global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup(Infrastructure.NormalizeBicepIdentifier(Name));

        consumerGroup.Name = ConsumerGroupName;

        return consumerGroup;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        writer.WriteString(nameof(Name), ConsumerGroupName);
    }
}
