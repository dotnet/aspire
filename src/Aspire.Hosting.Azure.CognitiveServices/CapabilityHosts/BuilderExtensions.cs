// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services capability host resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesCapabilityHostExtensions
{
    /// <summary>
    /// Adds an Azure Cognitive Services capability host resource to the account as the default for all projects.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services account resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services capability host resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCognitiveServicesCapabilityHostResource> AddDefaultCapabilityHost(
        this IResourceBuilder<AzureCognitiveServicesAccountResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var account = builder.Resource;
        AzureCognitiveServicesCapabilityHostResource? capabilityHostResource = null;

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var parentaccount = AzureCognitiveServicesAccountResource.GetProvisionableResource(
                infrastructure,
                account.GetBicepIdentifier()) ?? throw new InvalidOperationException($"Could not find parent Azure Cognitive Services project resource for project '{account.GetBicepIdentifier()}'."); 

            var capabilityHost = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, resourceName) =>
                {
                    var resource = CognitiveServicesCapabilityHost.FromExisting(identifier);
                    resource.Parent = parentaccount;
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var resource = new CognitiveServicesCapabilityHost(infra.AspireResource.GetBicepIdentifier())
                    {
                        Parent = parentaccount,
                        Name = name,
                        Properties = new CognitiveServicesCapabilityHostProperties
                        {
                            CapabilityHostKind = CapabilityHostKind.Agents,
                            Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                        }
                    };
                    return resource;
                });

            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = capabilityHost.Name });
        }

        capabilityHostResource = new AzureCognitiveServicesCapabilityHostResource(name, configureInfrastructure, account);
        return builder.ApplicationBuilder.AddResource(capabilityHostResource);
    }

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

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var parent = AzureCognitiveServicesProjectResource.GetProvisionableResource(
                infrastructure,
                project.GetBicepIdentifier()) ?? throw new InvalidOperationException($"Could not find parent Azure Cognitive Services project resource for project '{project.GetBicepIdentifier()}'."); 

            var capabilityHost = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
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

            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = capabilityHost.Name });
        }

        var capabilityHostResource = new AzureCognitiveServicesProjectCapabilityHostResource(name, configureInfrastructure, project);
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
}
