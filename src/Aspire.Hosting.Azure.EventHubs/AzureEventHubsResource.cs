// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.EventHubs;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Event Hubs resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Event Hubs resource.</param>
public class AzureEventHubsResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString,
    IResourceWithEndpoints,
    IResourceWithAzureFunctionsConfig
{
    private static readonly string[] s_eventHubClientNames =
    [
        "EventHubProducerClient",
        "EventHubConsumerClient",
        "EventProcessorClient",
        "PartitionReceiver",
        "EventHubBufferedProducerClient"
    ];

    private const string ConnectionKeyPrefix = "Aspire__Azure__Messaging__EventHubs";

    internal List<EventHub> Hubs { get; } = [];

    /// <summary>
    /// Gets the "eventHubsEndpoint" output reference from the bicep template for the Azure Event Hubs resource.
    /// </summary>
    public BicepOutputReference EventHubsEndpoint => new("eventHubsEndpoint", this);

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets a value indicating whether the Azure Event Hubs resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Event Hubs endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        IsEmulator
        ? ReferenceExpression.Create($"Endpoint=sb://{EmulatorEndpoint.Property(EndpointProperty.Host)}:{EmulatorEndpoint.Property(EndpointProperty.Port)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;")
        : ReferenceExpression.Create($"{EventHubsEndpoint}");

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (IsEmulator)
        {
            // Injected to support Azure Functions listener initialization.
            target[connectionName] = ConnectionStringExpression;
            // Injected to support Aspire client integration for each EventHubs client in Azure Functions projects.
            foreach (var clientName in s_eventHubClientNames)
            {
                target[$"{ConnectionKeyPrefix}__{clientName}__{connectionName}__ConnectionString"] = ConnectionStringExpression;
            }
        }
        else
        {
            // Injected to support Azure Functions listener initialization.
            target[$"{connectionName}__fullyQualifiedNamespace"] = EventHubsEndpoint;
            // Injected to support Aspire client integration for each EventHubs client in Azure Functions projects.
            foreach (var clientName in s_eventHubClientNames)
            {
                target[$"{ConnectionKeyPrefix}__{clientName}__{connectionName}__FullyQualifiedNamespace"] = EventHubsEndpoint;
            }
        }
    }
}
