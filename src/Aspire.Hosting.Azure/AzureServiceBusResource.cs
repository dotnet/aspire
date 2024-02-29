// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureServiceBusResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.servicebus.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Queues { get; } = [];
    internal Dictionary<string, string[]> Topics { get; } = [];

    /// <summary>
    /// Gets the "serviceBusEndpoint" output reference from the bicep template for the Azure Service Bus endpoint.
    /// </summary>
    public BicepOutputReference ServiceBusEndpoint => new("serviceBusEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Service Bus endpoint.
    /// </summary>
    public string ConnectionStringExpression => ServiceBusEndpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Service Bus endpoint.
    /// </summary>
    /// <returns>The connection string for the Azure Service Bus endpoint.</returns>
    public string? GetConnectionString() => ServiceBusEndpoint.Value;
}
