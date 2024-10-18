// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Service Bus resource.</param>
public class AzureServiceBusResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    internal List<string> Queues { get; } = [];
    internal List<string> Topics { get; } = [];
    internal List<(string TopicName, string Name)> Subscriptions { get; } = [];

    /// <summary>
    /// Gets the "serviceBusEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference ServiceBusEndpoint => new("serviceBusEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Service Bus endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ServiceBusEndpoint}");

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        // Injected to support Azure Functions listener initialization.
        target[$"{connectionName}__fullyQualifiedNamespace"] = ServiceBusEndpoint;
        // Injected to support Aspire client integration for Service Bus in Azure Functions projects.
        target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__FullyQualifiedNamespace"] = ServiceBusEndpoint;
    }
}
