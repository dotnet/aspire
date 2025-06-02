#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.OperationalInsights;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Log Analytics Workspace resources to the application model.
/// </summary>
public static class AzureLogAnalyticsWorkspaceExtensions
{
    /// <summary>
    /// Adds an Azure Log Analytics Workspace resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> AddAzureLogAnalyticsWorkspace(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var workspace = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = OperationalInsightsWorkspace.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new OperationalInsightsWorkspace(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Sku = new OperationalInsightsWorkspaceSku()
                    {
                        Name = OperationalInsightsWorkspaceSkuName.PerGB2018
                    },
                    Tags = { { "aspire-resource-name", name } }
                });

            infrastructure.Add(new ProvisioningOutput("logAnalyticsWorkspaceId", typeof(string))
            {
                Value = workspace.Id
            });
            
            // Add name output for the resource to externalize role assignments
            infrastructure.Add(new ProvisioningOutput("name", typeof(string))
            {
                Value = workspace.Name
            });
        };

        var resource = new AzureLogAnalyticsWorkspaceResource(name, configureInfrastructure);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Configures a resource that implements <see cref="IComputeEnvironmentResource"/> to use the specified Log Analytics Workspace.
    /// </summary>
    /// <typeparam name="T">The resource type that implements <see cref="IComputeEnvironmentResource"/>.</typeparam>
    /// <param name="builder">The resource builder for a resource that implements <see cref="IComputeEnvironmentResource"/>.</param>
    /// <param name="workspaceBuilder">The resource builder for the <see cref="AzureLogAnalyticsWorkspaceResource"/> to use.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="workspaceBuilder"/> is null.</exception>
    public static IResourceBuilder<T> WithAzureLogAnalyticsWorkspace<T>(this IResourceBuilder<T> builder, IResourceBuilder<AzureLogAnalyticsWorkspaceResource> workspaceBuilder)
        where T : IResource, IComputeEnvironmentResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(workspaceBuilder);

        // Add a LogAnalyticsWorkspaceReferenceAnnotation to indicate that the resource is using a specific workspace
        builder.WithAnnotation(new AzureLogAnalyticsWorkspaceReferenceAnnotation(workspaceBuilder.Resource));

        return builder;
    }
}
