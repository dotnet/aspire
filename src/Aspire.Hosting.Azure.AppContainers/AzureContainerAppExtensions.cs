// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.OperationalInsights;
using Azure.Provisioning.Roles;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using FileShare = Azure.Provisioning.Storage.FileShare;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Azure Container App definitions for projects.
/// </summary>
public static class AzureContainerAppExtensions
{
    /// <summary>
    /// Adds the necessary infrastructure for Azure Container Apps to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    public static IDistributedApplicationBuilder AddAzureContainerAppsInfrastructure(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // ensure AzureProvisioning is added first so the AzureResourcePreparer lifecycle hook runs before AzureContainerAppsInfrastructure
        builder.AddAzureProvisioning();

        // AzureContainerAppsInfrastructure will handle adding role assignments,
        // so Azure resources don't need to add the default role assignments themselves
        builder.Services.Configure<AzureProvisioningOptions>(o => o.SupportsTargetedRoleAssignments = true);

        builder.Services.TryAddLifecycleHook<AzureContainerAppsInfrastructure>();

        return builder;
    }

    /// <summary>
    /// Adds a container app environment resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns><see cref="IResourceBuilder{T}"/></returns>
    public static IResourceBuilder<AzureContainerAppEnvironmentResource> AddAzureContainerAppEnvironment(this IDistributedApplicationBuilder builder, string name)
    {
        builder.AddAzureContainerAppsInfrastructure();

        // Only support one temporarily until we can support multiple environments
        // and allowing each container app to be explicit about which environment it uses
        var existingContainerAppEnvResource = builder.Resources.OfType<AzureContainerAppEnvironmentResource>().FirstOrDefault();

        if (existingContainerAppEnvResource != null)
        {
            throw new NotSupportedException($"Only one container app environment is supported at this time. Found: {existingContainerAppEnvResource.Name}");
        }

        var containerAppEnvResource = new AzureContainerAppEnvironmentResource(name, static infra =>
        {
            var userPrincipalId = new ProvisioningParameter("userPrincipalId", typeof(string));

            infra.Add(userPrincipalId);

            var tags = new ProvisioningParameter("tags", typeof(object))
            {
                Value = new BicepDictionary<string>()
            };

            infra.Add(tags);

            var identity = new UserAssignedIdentity("mi")
            {
                Tags = tags
            };

            infra.Add(identity);

            var containerRegistry = new ContainerRegistryService("acr")
            {
                Sku = new() { Name = ContainerRegistrySkuName.Basic },
                Tags = tags
            };

            infra.Add(containerRegistry);

            var pullRa = containerRegistry.CreateRoleAssignment(ContainerRegistryBuiltInRole.AcrPull, identity);

            // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
            pullRa.Name = BicepFunction.CreateGuid(containerRegistry.Id, identity.Id, pullRa.RoleDefinitionId);
            infra.Add(pullRa);

            var laWorkspace = new OperationalInsightsWorkspace("law")
            {
                Sku = new() { Name = OperationalInsightsWorkspaceSkuName.PerGB2018 },
                Tags = tags
            };

            infra.Add(laWorkspace);

            var containerAppEnvironment = new ContainerAppManagedEnvironment("cae")
            {
                WorkloadProfiles = [
                    new ContainerAppWorkloadProfile()
                    {
                        WorkloadProfileType = "Consumption",
                        Name = "consumption"
                    }
                ],
                AppLogsConfiguration = new()
                {
                    Destination = "log-analytics",
                    LogAnalyticsConfiguration = new()
                    {
                        CustomerId = laWorkspace.CustomerId,
                        SharedKey = laWorkspace.GetKeys().PrimarySharedKey
                    }
                },
                Tags = tags
            };

            infra.Add(containerAppEnvironment);

            var dashboard = new ContainerAppEnvironmentDotnetComponentResource("aspireDashboard", "2024-10-02-preview")
            {
                Name = "aspire-dashboard",
                ComponentType = "AspireDashboard",
                Parent = containerAppEnvironment
            };

            infra.Add(dashboard);

            var roleAssignment = containerAppEnvironment.CreateRoleAssignment(AppContainersBuiltInRole.Contributor,
                RoleManagementPrincipalType.ServicePrincipal,
                userPrincipalId);

            // We need to set the principal type to null to let ARM infer the principal id
            roleAssignment.PrincipalType.ClearValue();

            infra.Add(roleAssignment);

            var managedStorages = new Dictionary<string, ContainerAppManagedEnvironmentStorage>();

            var resource = (AzureContainerAppEnvironmentResource)infra.AspireResource;

            if (resource.VolumeNames.Count > 0)
            {
                var storageVolume = new StorageAccount("storageVolume")
                {
                    Tags = tags,
                    Sku = new StorageSku() { Name = StorageSkuName.StandardLrs },
                    Kind = StorageKind.StorageV2,
                    LargeFileSharesState = LargeFileSharesState.Enabled
                };

                infra.Add(storageVolume);

                var storageVolumeFileService = new FileService("storageVolumeFileService")
                {
                    Parent = storageVolume
                };

                infra.Add(storageVolumeFileService);

                foreach (var (outputName, output) in resource.VolumeNames)
                {
                    var shareName = Infrastructure.NormalizeBicepIdentifier($"shares_{outputName}");
                    var managedStorageName = Infrastructure.NormalizeBicepIdentifier($"managedStorage_{outputName}");

                    var share = new FileShare(shareName)
                    {
                        Parent = storageVolumeFileService,
                        ShareQuota = 1024,
                        EnabledProtocol = FileShareEnabledProtocol.Smb
                    };

                    infra.Add(share);

                    var keysExpr = storageVolume.GetKeys()[0].Compile();
                    var keyValue = new MemberExpression(keysExpr, "value");

                    var containerAppStorage = new ContainerAppManagedEnvironmentStorage(managedStorageName)
                    {
                        Parent = containerAppEnvironment,
                        ManagedEnvironmentStorageAzureFile = new()
                        {
                            ShareName = share.Name,
                            AccountName = storageVolume.Name,
                            AccountKey = keyValue,
                            AccessMode = ContainerAppAccessMode.ReadWrite
                        }
                    };

                    infra.Add(containerAppStorage);

                    managedStorages[outputName] = containerAppStorage;
                }
            }

            // Add the volume outputs to the container app environment storage
            foreach (var (key, value) in managedStorages)
            {
                infra.Add(new ProvisioningOutput(key, typeof(string))
                {
                    Value = value.Name
                });
            }

            infra.Add(new ProvisioningOutput("MANAGED_IDENTITY_NAME", typeof(string))
            {
                Value = identity.Name
            });

            infra.Add(new ProvisioningOutput("MANAGED_IDENTITY_PRINCIPAL_ID", typeof(string))
            {
                Value = identity.PrincipalId
            });

            infra.Add(new ProvisioningOutput("AZURE_LOG_ANALYTICS_WORKSPACE_NAME", typeof(string))
            {
                Value = laWorkspace.Name
            });

            infra.Add(new ProvisioningOutput("AZURE_LOG_ANALYTICS_WORKSPACE_ID", typeof(string))
            {
                Value = laWorkspace.Id
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_NAME", typeof(string))
            {
                Value = containerRegistry.Name
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_ENDPOINT", typeof(string))
            {
                Value = containerRegistry.LoginServer
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", typeof(string))
            {
                Value = identity.Id
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME", typeof(string))
            {
                Value = containerAppEnvironment.Name
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", typeof(string))
            {
                Value = containerAppEnvironment.Id
            });

            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", typeof(string))
            {
                Value = containerAppEnvironment.DefaultDomain
            });
        });

        if (builder.ExecutionContext.IsRunMode)
        {
            // HACK: We need to return a valid resource builder for the container app environment
            // but in run mode, we don't want to add the resource to the builder.
            return builder.CreateResourceBuilder(containerAppEnvResource);
        }

        return builder.AddResource(containerAppEnvResource);
    }
}
