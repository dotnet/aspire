// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Service Bus Queue resource.
/// </summary>
public class AzureServiceBusQueueResource : Resource, IResourceWithConnectionString, IResourceWithParent<AzureServiceBusResource>
{
    /// <summary>
    /// Represents an Azure Service Bus Queue resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="namespace">The <see cref="AzureServiceBusResource"/> that the resource is stored in.</param>
    public AzureServiceBusQueueResource(string name, AzureServiceBusResource @namespace) : base(name)
    {
        Parent = @namespace;
        Parent.AddQueue(this);
    }

    /// <inheritdoc/>
    public AzureServiceBusResource Parent { get; }

    /// <inheritdoc/>
    public string? GetConnectionString() => Parent.GetConnectionString();
}
