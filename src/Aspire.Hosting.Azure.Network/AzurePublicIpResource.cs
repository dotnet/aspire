// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Public IP Address resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Public IP Address resource.</param>
public class AzurePublicIpResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure)
{
    /// <summary>
    /// Gets the "id" output reference from the Azure Public IP Address resource.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutput => new("name", this);

    /// <summary>
    /// Gets the "ipAddress" output reference from the Azure Public IP Address resource.
    /// </summary>
    public BicepOutputReference IpAddress => new("ipAddress", this);

    /// <summary>
    /// Gets or sets the public IP allocation method.
    /// </summary>
    public string? AllocationMethod { get; set; }

    /// <summary>
    /// Gets or sets the SKU for the public IP address.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the DNS name for the public IP address.
    /// </summary>
    public string? DnsName { get; set; }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();
        
        // Check if a PublicIPAddress with the same identifier already exists
        var existingIp = resources.OfType<PublicIPAddress>().SingleOrDefault(ip => ip.BicepIdentifier == bicepIdentifier);
        
        if (existingIp is not null)
        {
            return existingIp;
        }
        
        // Create and add new resource if it doesn't exist
        var publicIp = PublicIPAddress.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            publicIp))
        {
            publicIp.Name = NameOutput.AsProvisioningParameter(infra);
        }

        infra.Add(publicIp);
        return publicIp;
    }
}
