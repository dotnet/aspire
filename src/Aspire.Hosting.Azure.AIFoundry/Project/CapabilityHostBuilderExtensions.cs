// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Resources;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services capability host resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesCapabilityHostExtensions
{

    /// <summary>
    /// Adds an Azure Cognitive Services capability host resource to the project.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services project resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services capability host resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectCapabilityHostResource> AddCapabilityHost(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var project = builder.Resource;
        AzureCognitiveServicesProjectCapabilityHostResource? capabilityHostResource;
        void configureInfrastructure(AzureResourceInfrastructure infra)
        {
            var parent = AzureCognitiveServicesProjectResource.GetProvisionableResource(
                infra,
                project.GetBicepIdentifier()) ?? throw new InvalidOperationException($"Could not find parent Azure Cognitive Services project resource for project '{project.GetBicepIdentifier()}'.");
            var myBicepId = infra.AspireResource.GetBicepIdentifier();
            var capabilityHost = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infra,
                (identifier, resourceName) =>
                {
                    var resource = CognitiveServicesProjectCapabilityHost.FromExisting(identifier);
                    resource.Parent = parent;
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var resource = new CognitiveServicesProjectCapabilityHost(infra.AspireResource.GetBicepIdentifier())
                    {
                        Parent = parent,
                        Name = name,
                        Properties = new CognitiveServicesCapabilityHostProperties
                        {
                            CapabilityHostKind = CapabilityHostKind.Agents,
                            Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                        }
                    };
                    return resource;
                });
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = capabilityHost.Name });

            if (capabilityHostResource?.KeyVault != null)
            {
                capabilityHostResource.KeyVault.AddAsExistingResource(infra);
                // TODO: Add Key Vault connection to project and set connection property
            }

            if (capabilityHostResource?.Storage != null)
            {
                capabilityHostResource.Storage.AddAsExistingResource(infra);
                // TODO: Add storage connection to project and set connection property
            }
            if (capabilityHostResource?.CosmosDB != null)
            {
                capabilityHostResource.CosmosDB.AddAsExistingResource(infra);
                // TODO: Add CosmosDB connection to project and set connection property
            }
            if (capabilityHostResource?.VirtualNetwork != null)
            {
                capabilityHostResource.VirtualNetwork.AddAsExistingResource(infra);
                // TODO: Configure capability host to use virtual network
            }
            // TODO: Add other resources as needed, like AI Search, AOAI
        };
        capabilityHostResource = new AzureCognitiveServicesProjectCapabilityHostResource(name, configureInfrastructure, project);
        return builder.ApplicationBuilder.AddResource(capabilityHostResource);
    }

    /// <summary>
    /// Configures the Azure Cognitive Services project capability host resource to use a virtual network.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="subnetId"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectCapabilityHostResource> WithVirtualNetwork(
        this IResourceBuilder<AzureCognitiveServicesProjectCapabilityHostResource> builder,
        string subnetId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(subnetId);

        return builder.WithConfiguration((CognitiveServicesProjectCapabilityHost capHost) =>
        {
            // TODO: Add virtual network
            // Then update capHost props
            // TODO: This property doesn't exist yet?
            // capHost.Properties.EnablePublicHostingEndpoint = false;
        });
    }

    /// <summary>
    /// Configures the Azure Cognitive Services project capability host resource to use an Azure storage account.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectCapabilityHostResource> WithBlobStorage(
        this IResourceBuilder<AzureCognitiveServicesProjectCapabilityHostResource> builder,
        IResourceBuilder<AzureStorageResource> storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        return builder.WithConfiguration((CognitiveServicesProjectCapabilityHost capHost) =>
        {
            //capHost.Properties.StorageConnections;
            // TODO: Add virtual network
            // Then update capHost props
            // TODO: This property doesn't exist yet?
            // capHost.Properties.EnablePublicHostingEndpoint = false;
        });
    }
}
