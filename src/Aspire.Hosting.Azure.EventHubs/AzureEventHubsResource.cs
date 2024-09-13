// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.EventHubs;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Event Hubs resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to configure the Azure Event Hubs resource.</param>
public class AzureEventHubsResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithConnectionString,
    IResourceWithEndpoints,
    IResourceWithAzureFunctionsConfig
{
    internal List<(string Name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHub>? Configure)> Hubs { get; } = [];

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
            target[connectionName] = ConnectionStringExpression;
        }
        else
        {
            target[$"{connectionName}__fullyQualifiedNamespace"] = EventHubsEndpoint;
        }
    }
}
