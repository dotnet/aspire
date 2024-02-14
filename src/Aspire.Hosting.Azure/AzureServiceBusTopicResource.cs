// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Service Bus Topic resource.
/// </summary>
public class AzureServiceBusTopicResource : Resource, IResourceWithConnectionString, IResourceWithParent<AzureServiceBusResource>
{
    /// <summary>
    /// Represents an Azure Service Bus Topic resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    /// <param name="namespace">The <see cref="AzureServiceBusResource"/> that the resource is stored in.</param>
    public AzureServiceBusTopicResource(string name, string[] subscriptions, AzureServiceBusResource @namespace) : base(name)
    {
        Subscriptions = subscriptions;
        Parent = @namespace;
        Parent.AddTopic(this);
    }

    /// <inheritdoc/>
    public AzureServiceBusResource Parent { get; }

    /// <summary>
    /// Gets the list of subscriptions of the Azure Service Bus Topic resource.
    /// </summary>
    public string[] Subscriptions { get; }

    /// <inheritdoc/>
    public string? GetConnectionString() => Parent.GetConnectionString();
}
