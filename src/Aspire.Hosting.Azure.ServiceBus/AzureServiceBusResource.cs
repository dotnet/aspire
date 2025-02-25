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
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString, IResourceWithAzureFunctionsConfig, IResourceWithEndpoints
{
    internal List<AzureServiceBusQueueResource> Queues { get; } = [];
    internal List<AzureServiceBusTopicResource> Topics { get; } = [];

    /// <summary>
    /// Gets the "serviceBusEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference ServiceBusEndpoint => new("serviceBusEndpoint", this);

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets a value indicating whether the Azure Service Bus resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Service Bus endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        IsEmulator
        ? ReferenceExpression.Create($"Endpoint=sb://{EmulatorEndpoint.Property(EndpointProperty.HostAndPort)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;")
        : ReferenceExpression.Create($"{ServiceBusEndpoint}");

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (IsEmulator)
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}"] = ConnectionStringExpression;
            // Injected to support Aspire client integration for Service Bus in Azure Functions projects.
            target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__ConnectionString"] = ConnectionStringExpression;
        }
        else
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}__fullyQualifiedNamespace"] = ServiceBusEndpoint;
            // Injected to support Aspire client integration for Service Bus in Azure Functions projects.
            target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__FullyQualifiedNamespace"] = ServiceBusEndpoint;
        }
    }
}
