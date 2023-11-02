// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureServiceBusResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the Service Bus endpoint. This is the full uri to the service bus 
    /// namespace, for example <c>"namespace.servicebus.windows.net"</c>.
    /// </summary>
    public string? ServiceBusEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the names of the queues associated with the Azure Service Bus resource.
    /// </summary>
    public string[] QueueNames { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the names of the topics associated with the Azure Service Bus resource.
    /// </summary>
    public string[] TopicNames { get; set; } = [];

    /// <summary>
    /// Gets the connection string for the Azure Service Bus endpoint.
    /// </summary>
    /// <returns>The connection string for the Azure Service Bus endpoint.</returns>
    public string? GetConnectionString() => ServiceBusEndpoint;
}
