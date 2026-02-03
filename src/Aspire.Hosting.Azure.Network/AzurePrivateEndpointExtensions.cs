// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Network;
using Azure.Core;
using Azure.Provisioning.PrivateDns;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Private Endpoint resources to the application model.
/// </summary>
public static class AzurePrivateEndpointExtensions
{
    /// <summary>
    /// Adds an Azure Private Endpoint resource to the subnet.
    /// </summary>
    /// <param name="subnet">The subnet to add the private endpoint to.</param>
    /// <param name="target">The target Azure resource to connect via private link.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzurePrivateEndpointResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method automatically creates the Private DNS Zone, VNet Link, and DNS Zone Group
    /// required for private endpoint DNS resolution.
    /// </para>
    /// <para>
    /// When a private endpoint is added, the target resource (or its parent) is automatically
    /// configured to deny public network access. To override this behavior, use
    /// <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}"/> to customize
    /// the network settings.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<AzurePrivateEndpointResource> AddPrivateEndpoint(
        this IResourceBuilder<AzureSubnetResource> subnet,
        IResourceBuilder<IAzurePrivateEndpointTarget> target)
    {
        ArgumentNullException.ThrowIfNull(subnet);
        ArgumentNullException.ThrowIfNull(target);

        var builder = subnet.ApplicationBuilder;
        var name = $"{subnet.Resource.Name}-{target.Resource.Name}-pe";

        var resource = new AzurePrivateEndpointResource(name, ConfigurePrivateEndpoint)
        {
            Subnet = subnet.Resource,
            Target = target.Resource
        };

        if (builder.ExecutionContext.IsRunMode)
        {
            // In run mode, we don't want to add the resource to the builder.
            return builder.CreateResourceBuilder(resource);
        }

        // Add annotation to the target's root parent (e.g., storage account) to signal
        // that it should deny public network access.
        // This should only be done in publish mode. In run mode, the target resource
        // needs to be accessible over the public internet so the local app can reach it.
        IResource rootResource = target.Resource;
        while (rootResource is IResourceWithParent parentedResource)
        {
            rootResource = parentedResource.Parent;
        }
        rootResource.Annotations.Add(new PrivateEndpointTargetAnnotation());

        return builder.AddResource(resource);

        void ConfigurePrivateEndpoint(AzureResourceInfrastructure infra)
        {
            var azureResource = (AzurePrivateEndpointResource)infra.AspireResource;

            // Create Private DNS Zone for the target service
            var dnsZoneName = azureResource.Target!.GetPrivateDnsZoneName();
            var dnsZoneIdentifier = Infrastructure.NormalizeBicepIdentifier(dnsZoneName.Replace(".", "_"));

            var privateDnsZone = new PrivateDnsZone(dnsZoneIdentifier)
            {
                Name = dnsZoneName,
                Location = new AzureLocation("global"),
                Tags = { { "aspire-resource-name", $"{azureResource.Name}-dns" } }
            };
            infra.Add(privateDnsZone);

            // Create VNet Link to connect DNS zone to the VNet
            var vnetLinkIdentifier = $"{dnsZoneIdentifier}_vnetlink";
            var vnetLink = new VirtualNetworkLink(vnetLinkIdentifier)
            {
                Name = $"{azureResource.Name}-vnet-link",
                Parent = privateDnsZone,
                Location = new AzureLocation("global"),
                RegistrationEnabled = false,
                VirtualNetworkId = azureResource.Subnet!.Parent.Id.AsProvisioningParameter(infra),
                Tags = { { "aspire-resource-name", $"{azureResource.Name}-vnetlink" } }
            };
            infra.Add(vnetLink);

            // Create the Private Endpoint
            var endpoint = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
                (identifier, name) =>
                {
                    var resource = PrivateEndpoint.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var pe = new PrivateEndpoint(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };

                    // Configure subnet
                    pe.Subnet.Id = azureResource.Subnet.Id.AsProvisioningParameter(infrastructure);

                    // Configure private link service connection
                    pe.PrivateLinkServiceConnections.Add(
                        new NetworkPrivateLinkServiceConnection
                        {
                            Name = $"{azureResource.Name}-connection",
                            PrivateLinkServiceId = azureResource.Target.Id.AsProvisioningParameter(infrastructure),
                            GroupIds = [.. azureResource.Target.GetPrivateLinkGroupIds()]
                        });

                    return pe;
                });

            // Create DNS Zone Group on the Private Endpoint
            var dnsZoneGroupIdentifier = $"{endpoint.BicepIdentifier}_dnsgroup";
            var dnsZoneGroup = new PrivateDnsZoneGroup(dnsZoneGroupIdentifier)
            {
                Name = "default",
                Parent = endpoint,
                PrivateDnsZoneConfigs =
                {
                    new PrivateDnsZoneConfig
                    {
                        Name = dnsZoneIdentifier,
                        PrivateDnsZoneId = privateDnsZone.Id
                    }
                }
            };
            infra.Add(dnsZoneGroup);

            // Output the Private Endpoint ID for references
            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = endpoint.Id
            });

            // We need to output name so it can be referenced by others.
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = endpoint.Name });
        }
    }
}
