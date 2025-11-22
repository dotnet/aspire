// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure NAT Gateway resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure NAT Gateway resource.</param>
public class AzureNatGatewayResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure)
{
    internal List<AzurePublicIpResource> PublicIpAddresses { get; } = [];

    /// <summary>
    /// Gets the "id" output reference from the Azure NAT Gateway resource.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutput => new("name", this);

    /// <summary>
    /// Gets or sets the idle timeout in minutes for the NAT Gateway (4-120 minutes).
    /// </summary>
    public int? IdleTimeoutInMinutes { get; set; }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();
        
        // Check if a NatGateway with the same identifier already exists
        var existingNatGw = resources.OfType<NatGateway>().SingleOrDefault(natgw => natgw.BicepIdentifier == bicepIdentifier);
        
        if (existingNatGw is not null)
        {
            return existingNatGw;
        }
        
        // Create and add new resource if it doesn't exist
        var natGw = NatGateway.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            natGw))
        {
            natGw.Name = NameOutput.AsProvisioningParameter(infra);
        }

        infra.Add(natGw);
        return natGw;
    }
}
