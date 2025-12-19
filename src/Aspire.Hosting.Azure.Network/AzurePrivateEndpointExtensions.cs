// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Network;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Private Endpoint resources to the application model.
/// </summary>
public static class AzurePrivateEndpointExtensions
{
    /// <summary>
    /// Adds an Azure Private Endpoint resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="subnet">The subnet associated with the private endpoint.</param>
    /// <param name="target">The name of the Azure Private Endpoint resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzurePrivateEndpointResource}"/>.</returns>
    public static IResourceBuilder<AzurePrivateEndpointResource> AddAzurePrivateEndpoint(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureSubnetResource> subnet,
        IResourceBuilder<IAzurePrivateEndpointTarget> target)
    {
        ArgumentNullException.ThrowIfNull(builder);
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

        return builder.AddResource(resource);

        static void ConfigurePrivateEndpoint(AzureResourceInfrastructure infra)
        {
            var endpoint = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
                (identifier, name) =>
                {
                    var resource = PrivateEndpoint.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var azureResource = (AzurePrivateEndpointResource)infrastructure.AspireResource;
                    var endpoint = new PrivateEndpoint(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };

                    // Configure subnet if specified
                    if (azureResource.Subnet is not null)
                    {
                        endpoint.Subnet.Id = azureResource.Subnet.Id.AsProvisioningParameter(infrastructure);
                    }

                    // Configure private link service connection if target resource is specified
                    if (azureResource.Target is not null)
                    {
                        endpoint.PrivateLinkServiceConnections.Add(
                            new NetworkPrivateLinkServiceConnection
                            {
                                Name = $"{azureResource.Name}-connection",
                                PrivateLinkServiceId = azureResource.Target.Id.AsProvisioningParameter(infrastructure),
                                GroupIds = [.. azureResource.Target.GetPrivateLinkGroupIds()]
                            });
                    }

                    return endpoint;
                });

            var azureResource = (AzurePrivateEndpointResource)infra.AspireResource;

            // Add private DNS zone groups
            // TODO: Enable this once Private DNS Zone Groups are supported in the provisioning library
            //if (azureResource.PrivateDnsZoneGroups.Count > 0)
            //{
            //    foreach (var group in azureResource.PrivateDnsZoneGroups)
            //    {
            //        var cdkGroup = group.ToProvisioningEntity(infra);
            //        cdkGroup.Parent = endpoint;
            //        infra.Add(cdkGroup);
            //    }
            //}

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
