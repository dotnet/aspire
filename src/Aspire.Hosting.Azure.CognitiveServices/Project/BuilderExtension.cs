// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services project resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesProjectExtensions
{

    /// <summary>
    /// Adds an Azure Cognitive Services project resource to the application model.
    ///
    /// This will also attach the project as a deployment target for agents.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services account resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services project resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the Azure Cognitive Services project resource.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> AddProject(
        this IResourceBuilder<AzureCognitiveServicesAccountResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        void configureInfrastructure(AzureResourceInfrastructure infra)
        {
            var prefix = infra.AspireResource.Name;
            var aspireResource = (AzureCognitiveServicesProjectResource)infra.AspireResource;
            var tags = new ProvisioningParameter("tags", typeof(object))
            {
                Value = new BicepDictionary<string>()
            };
            infra.Add(tags);

            // This tells azd to avoid creating infrastructure
            var userPrincipalId = new ProvisioningParameter(AzureBicepResource.KnownParameters.UserPrincipalId, typeof(string)) { Value = new BicepValue<string>(string.Empty) };
            infra.Add(userPrincipalId);

            // This is the principal used for the app runtime
            var identity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{prefix}-mi"))
            {
                Tags = tags
            };
            infra.Add(identity);

            // Use a user-provided container registry or create a new one.
            // The container registry is used to host images for hosted agents.
            ContainerRegistryService? containerRegistry = null;
            if (aspireResource.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var registryReferenceAnnotation) && registryReferenceAnnotation.Registry is AzureProvisioningResource registry)
            {
                containerRegistry = (ContainerRegistryService)registry.AddAsExistingResource(infra);
            }
            else
            {
                containerRegistry = new ContainerRegistryService(Infrastructure.NormalizeBicepIdentifier($"{prefix}_acr"))
                {
                    Sku = new() { Name = ContainerRegistrySkuName.Basic },
                    Tags = tags
                };
            }
            infra.Add(containerRegistry);
            // TODO: re-enable once role assignment permissions issues are fixed.
            // var pullRa = containerRegistry.CreateRoleAssignment(ContainerRegistryBuiltInRole.AcrPull, identity);
            // // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
            // pullRa.Name = CreateGuid(containerRegistry.Id, identity.Id, pullRa.RoleDefinitionId);
            // infra.Add(pullRa);

            var account = builder.Resource.AddAsExistingResource(infra);
            // Create the Cognitive Services project resource
            var project = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infra,
                (identifier, resourceName) =>
                {
                    var resource = aspireResource.FromExisting(identifier);
                    resource.Parent = account;
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var resource = new CognitiveServicesProject(infra.AspireResource.GetBicepIdentifier())
                    {
                        Parent = account,
                        Name = name,
                        Identity = new ManagedServiceIdentity()
                        {
                            ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
                        },
                        // TODO: Keys must be IDs, but UserAssignedIdentities expects string keys, whereas we
                        // have BicepValue Ids. Not sure how to resolve this yet.
                        // Identity = new ManagedServiceIdentity()
                        // {
                        //     UserAssignedIdentities = { { identity.Id, new UserAssignedIdentityDetails() } }
                        // },
                        Properties = new CognitiveServicesProjectProperties
                        {
                            DisplayName = name
                        },
                        Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                    };
                    return resource;
                });
            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = project.Id
            });
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = project.Name });
            infra.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = (BicepValue<string>)new IndexExpression((BicepExpression)project.Properties.Endpoints!, "AI Foundry API")
            });
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_ENDPOINT", typeof(string))
            {
                Value = containerRegistry.LoginServer
            });
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_NAME", typeof(string))
            {
                Value = containerRegistry.Name
            });
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", typeof(string))
            {
                Value = identity.Id
            });
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", typeof(string))
            {
                Value = identity.ClientId
            });
        }
        var resource = new AzureCognitiveServicesProjectResource(name, configureInfrastructure, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Associates a container registry with the Azure Cognitive Services project resource for
    /// publishing and locating hosted agents.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> WithContainerRegistry(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureContainerRegistryResource> registryBuilder)
    {
        return builder.WithContainerRegistry(registryBuilder.Resource);
    }

    /// <summary>
    /// Associates a container registry with the Azure Cognitive Services project resource for
    /// publishing and locating hosted agents.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> WithContainerRegistry(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        AzureContainerRegistryResource registry)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registry);

        // This will be queried during the "publish" phase
        builder.Resource.Annotations.Add(new ContainerRegistryReferenceAnnotation(registry));
        return builder;
    }

    /// <summary>
    /// Adds a reference to an Azure Cognitive Services project resource to the destination resource.
    ///
    /// This adds both the standard environment variables (e.g. `ConnectionStrings__{name}={url}`) but also
    /// the `AZURE_AI_PROJECT_ENDPOINT` environment variable.
    /// </summary>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<AzureCognitiveServicesProjectResource> project)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(project);

        // Add standard references and environment variables
        ResourceBuilderExtensions.WithReference(builder, project);

        var resource = project.Resource;

        // Determine what to inject based on the annotation on the destination resource
        var injectionAnnotation = builder.Resource.TryGetLastAnnotation<ReferenceEnvironmentInjectionAnnotation>(out var annotation) ? annotation : null;
        var flags = injectionAnnotation?.Flags ?? ReferenceEnvironmentInjectionFlags.All;

        if (flags.HasFlag(ReferenceEnvironmentInjectionFlags.ConnectionString))
        {
            builder.WithEnvironment(
                resource.ConnectionStringEnvironmentVariable ?? "AZURE_AI_PROJECT_ENDPOINT",
                resource.ConnectionStringExpression
            );
            builder.WithEnvironment("AGENT_PROJECT_RESOURCE_ID", resource.Id);
        }
        if (builder is IResourceBuilder<IResourceWithWaitSupport> waitableBuilder)
        {
            waitableBuilder.WaitFor(project);
        }
        return builder;
    }
}
