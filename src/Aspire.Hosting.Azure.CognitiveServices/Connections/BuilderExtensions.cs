// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services connection resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesConnectionsBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Cognitive Services connection resource to a project. This is a low level
    /// interface that requires the caller to specify all connection properties.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services project resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services connection resource.</param>
    /// <param name="configureProperties">Action to customize the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the Azure Cognitive Services connection resource.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectConnectionResource> AddConnection(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        string name,
        Func<AzureResourceInfrastructure, CognitiveServicesConnectionProperties> configureProperties)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var parent = builder.Resource;

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var aspireResource = (AzureCognitiveServicesProjectConnectionResource)infrastructure.AspireResource;
            var projectBicepId = parent.GetBicepIdentifier();
            var project = AzureCognitiveServicesProjectResource.GetProvisionableResource(
                infrastructure,
                projectBicepId) ?? throw new InvalidOperationException($"Could not find parent Azure Cognitive Services project resource for project '{projectBicepId}'.");
            var connection = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, resourceName) =>
                {
                    var resource = aspireResource.FromExisting(identifier);
                    resource.Parent = project;
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var resource = new CognitiveServicesProjectConnection(aspireResource.GetBicepIdentifier())
                    {
                        Parent = project,
                        Name = name,
                        Properties = configureProperties(infra)
                    };
                    return resource;
                });

            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = connection.Name });
        }
        var connectionResource = new AzureCognitiveServicesProjectConnectionResource(name, configureInfrastructure, parent);
        return builder.ApplicationBuilder.AddResource(connectionResource);
    }
}
