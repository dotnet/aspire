// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.ServiceBus;

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

    /// <summary>
    /// Gets the "name" output reference from the bicep template for the Azure Service Bus resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

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
        => ApplyAzureFunctionsConfiguration(target, connectionName);

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var sbNamespace = ServiceBusNamespace.FromExisting(this.GetBicepIdentifier());
        sbNamespace.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(sbNamespace);
        return sbNamespace;
    }

    internal ReferenceExpression GetConnectionString(string? queueOrTopicName, string? subscriptionName)
    {
        if (string.IsNullOrEmpty(queueOrTopicName) && string.IsNullOrEmpty(subscriptionName))
        {
            return ConnectionStringExpression;
        }

        var builder = new ReferenceExpressionBuilder();

        if (IsEmulator)
        {
            builder.AppendFormatted(ConnectionStringExpression);
        }
        else
        {
            builder.Append($"Endpoint={ConnectionStringExpression}");
        }

        if (!string.IsNullOrEmpty(queueOrTopicName))
        {
            builder.Append($";EntityPath={queueOrTopicName}");

            if (!string.IsNullOrEmpty(subscriptionName))
            {
                builder.Append($"/Subscriptions/{subscriptionName}");
            }
        }

        return builder.Build();
    }

    internal void ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName, string? queueOrTopicName = null, string? subscriptionName = null)
    {
        if (IsEmulator)
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}"] = ConnectionStringExpression;
            // Injected to support Aspire client integration for Service Bus in Azure Functions projects.
            target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__ConnectionString"] = GetConnectionString(queueOrTopicName, subscriptionName);
        }
        else
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}__fullyQualifiedNamespace"] = ServiceBusEndpoint;
            // Injected to support Aspire client integration for Service Bus in Azure Functions projects.
            target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__FullyQualifiedNamespace"] = ServiceBusEndpoint;
            if (queueOrTopicName != null)
            {
                target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__QueueOrTopicName"] = queueOrTopicName;
            }
            if (subscriptionName != null)
            {
                target[$"Aspire__Azure__Messaging__ServiceBus__{connectionName}__SubscriptionName"] = subscriptionName;
            }
        }
    }
}
