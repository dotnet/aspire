// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Virtual Network resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Virtual Network resource.</param>
public class AzureVirtualNetworkResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure)
{
    internal List<AzureSubnetResource> Subnets { get; } = [];

    /// <summary>
    /// Gets the "id" output reference from the Azure Virtual Network resource.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutput => new("name", this);

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();
        
        // Check if a VirtualNetwork with the same identifier already exists
        var existingVNet = resources.OfType<VirtualNetwork>().SingleOrDefault(vnet => vnet.BicepIdentifier == bicepIdentifier);
        
        if (existingVNet is not null)
        {
            return existingVNet;
        }
        
        // Create and add new resource if it doesn't exist
        var vnet = VirtualNetwork.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            vnet))
        {
            vnet.Name = NameOutput.AsProvisioningParameter(infra);
        }

        infra.Add(vnet);
        return vnet;
    }
}
