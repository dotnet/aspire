#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.ContainerRegistry;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Container Registry resources to the application model.
/// </summary>
public static class AzureContainerRegistryExtensions
{
    /// <summary>
    /// Adds an Azure Container Registry resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureContainerRegistryResource}"/> builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public static IResourceBuilder<AzureContainerRegistryResource> AddAzureContainerRegistry(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var registry = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = ContainerRegistryService.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new ContainerRegistryService(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Sku = new() { Name = ContainerRegistrySkuName.Basic },
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            infrastructure.Add(registry);

            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = registry.Name });
            infrastructure.Add(new ProvisioningOutput("loginServer", typeof(string)) { Value = registry.LoginServer });
        };

        var resource = new AzureContainerRegistryResource(name, configureInfrastructure);

        // Don't add the resource to the infrastructure if we're in run mode.
        if (builder.ExecutionContext.IsRunMode)
        {
            return builder.CreateResourceBuilder(resource);
        }

        return builder.AddResource(resource)
                .WithAnnotation(new DefaultRoleAssignmentsAnnotation(new HashSet<RoleDefinition>()));
    }

    /// <summary>
    /// Configures a resource that implements <see cref="IContainerRegistry"/> to use the specified Azure Container Registry.
    /// </summary>
    /// <typeparam name="T">The resource type that implements <see cref="IContainerRegistry"/>.</typeparam>
    /// <param name="builder">The resource builder for a resource that implements <see cref="IContainerRegistry"/>.</param>
    /// <param name="registryBuilder">The resource builder for the <see cref="AzureContainerRegistryResource"/> to use.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="registryBuilder"/> is null.</exception>
    public static IResourceBuilder<T> WithAzureContainerRegistry<T>(this IResourceBuilder<T> builder, IResourceBuilder<AzureContainerRegistryResource> registryBuilder)
        where T : IResource, IComputeEnvironmentResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registryBuilder);

        // Add a ContainerRegistryReferenceAnnotation to indicate that the resource is using a specific registry
        builder.WithAnnotation(new ContainerRegistryReferenceAnnotation(registryBuilder.Resource));

        return builder;
    }

    /// <summary>
    /// Adds role assignments to the specified Azure Container Registry resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource being configured.</typeparam>
    /// <param name="builder">The resource builder for the resource that will have role assignments.</param>
    /// <param name="target">The target Azure Container Registry resource.</param>
    /// <param name="roles">The roles to assign to the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureContainerRegistryResource> target,
        params ContainerRegistryBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, ContainerRegistryBuiltInRole.GetBuiltInRoleName, roles);
    }
}
