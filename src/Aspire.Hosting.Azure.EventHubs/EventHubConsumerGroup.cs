// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.EventHubs;

/// <summary>
/// Represents a Consumer Group.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class EventHubConsumerGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubConsumerGroup"/> class.
    /// </summary>
    public EventHubConsumerGroup(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The event hub name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.EventHubs.EventHub"/> instance.</returns>
    internal global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup ToProvisioningEntity()
    {
        var consumerGroup = new global::Azure.Provisioning.EventHubs.EventHubsConsumerGroup(Infrastructure.NormalizeBicepIdentifier(Name));

        consumerGroup.Name = Name;

        return consumerGroup;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        writer.WriteString(nameof(Name), Name);
    }
}
