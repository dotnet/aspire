// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.WebPubSub;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Web PubSub resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
public class AzureWebPubSubResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString
{
    internal Dictionary<string, AzureWebPubSubHubResource> Hubs { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the "endpoint" output reference from the bicep template for Azure Web PubSub.
    /// </summary>
    public BicepOutputReference Endpoint => new("endpoint", this);

    private BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the connection string template for the manifest for Azure Web PubSub.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Endpoint}");

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = WebPubSubService.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }
}
