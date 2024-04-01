// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.ServiceBus;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to configure the Azure Service Bus resource.</param>
public class AzureServiceBusResource(string name, Action<ResourceModuleConstruct> configureConstruct)
    : AzureConstructResource(name, configureConstruct), IResourceWithConnectionString
{
    internal List<(string Name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusQueue>? Configure)> Queues { get; } = [];
    internal List<(string Name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusTopic>? Configure)> Topics { get; } = [];
    internal List<(string TopicName, string Name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusSubscription>? Configure)> Subscriptions { get; } = [];

    /// <summary>
    /// Gets the "serviceBusEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference ServiceBusEndpoint => new("serviceBusEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Service Bus endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ServiceBusEndpoint}");
}
