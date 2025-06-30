// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppService;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Roles;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for adding Azure App Service Environment resources to a distributed application builder.
/// </summary>
public static partial class AzureAppServiceEnvironmentExtensions
{
    internal static IDistributedApplicationBuilder AddAzureAppServiceInfrastructureCore(this IDistributedApplicationBuilder builder)
    {
        // ensure AzureProvisioning is added first so the AzureResourcePreparer lifecycle hook runs before AzureAppServiceInfrastructure
        builder.AddAzureProvisioning();

        builder.Services.Configure<AzureProvisioningOptions>(options => options.SupportsTargetedRoleAssignments = true);

        builder.Services.TryAddLifecycleHook<AzureAppServiceInfrastructure>();

        return builder;
    }

    /// <summary>
    /// Adds a azure app service environment resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns><see cref="IResourceBuilder{T}"/></returns>
    public static IResourceBuilder<AzureAppServiceEnvironmentResource> AddAzureAppServiceEnvironment(this IDistributedApplicationBuilder builder, string name)
    {
        builder.AddAzureAppServiceInfrastructureCore();

        var resource = new AzureAppServiceEnvironmentResource(name, static infra =>
        {
            var prefix = infra.AspireResource.Name;
            var resource = infra.AspireResource;

            // This tells azd to avoid creating infrastructure
            var userPrincipalId = new ProvisioningParameter(AzureBicepResource.KnownParameters.UserPrincipalId, typeof(string));
            infra.Add(userPrincipalId);

            var tags = new ProvisioningParameter("tags", typeof(object))
            {
                Value = new BicepDictionary<string>()
            };

            infra.Add(tags);

            var identity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{prefix}-mi"))
            {
                Tags = tags
            };

            infra.Add(identity);

            ContainerRegistryService? containerRegistry = null;
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (resource.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var registryReferenceAnnotation) && registryReferenceAnnotation.Registry is AzureProvisioningResource registry)
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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

            var pullRa = containerRegistry.CreateRoleAssignment(ContainerRegistryBuiltInRole.AcrPull, identity);

            // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
            pullRa.Name = BicepFunction.CreateGuid(containerRegistry.Id, identity.Id, pullRa.RoleDefinitionId);
            infra.Add(pullRa);

            var plan = new AppServicePlan(Infrastructure.NormalizeBicepIdentifier($"{prefix}-asplan"))
            {
                Sku = new AppServiceSkuDescription
                {
                    Name = "P0V3",
                    Tier = "Premium"
                },
                Kind = "Linux",
                IsReserved = true
            };

            infra.Add(plan);

            infra.Add(new ProvisioningOutput("planId", typeof(string))
            {
                Value = plan.Id
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_NAME", typeof(string))
            {
                Value = containerRegistry.Name
            });

            // AZD looks for this output to find the container registry endpoint
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_ENDPOINT", typeof(string))
            {
                Value = containerRegistry.LoginServer
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", typeof(string))
            {
                Value = identity.Id
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", typeof(string))
            {
                Value = identity.ClientId
            });
        });

        if (!builder.ExecutionContext.IsPublishMode)
        {
            return builder.CreateResourceBuilder(resource);
        }

        return builder.AddResource(resource);
    }
}
