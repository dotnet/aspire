// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
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
    [Obsolete($"Use {nameof(AddAzureContainerAppEnvironment)} instead. This method will be removed in a future version.")]
    public static IDistributedApplicationBuilder AddAzureContainerAppsInfrastructure(this IDistributedApplicationBuilder builder) =>
        AddAzureContainerAppsInfrastructureCore(builder);

    private static IDistributedApplicationBuilder AddAzureContainerAppsInfrastructureCore(this IDistributedApplicationBuilder builder)
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
        builder.AddAzureContainerAppsInfrastructureCore();

        // Only support one temporarily until we can support multiple environments
        // and allowing each container app to be explicit about which environment it uses
        var existingContainerAppEnvResource = builder.Resources.OfType<AzureContainerAppEnvironmentResource>().FirstOrDefault();

        if (existingContainerAppEnvResource != null)
        {
            throw new NotSupportedException($"Only one container app environment is supported at this time. Found: {existingContainerAppEnvResource.Name}");
        }

        var containerAppEnvResource = new AzureContainerAppEnvironmentResource(name, static infra =>
        {
            var appEnvResource = (AzureContainerAppEnvironmentResource)infra.AspireResource;

            // This tells azd to avoid creating infrastructure
            var userPrincipalId = new ProvisioningParameter(AzureBicepResource.KnownParameters.UserPrincipalId, typeof(string));
            infra.Add(userPrincipalId);

            var tags = new ProvisioningParameter("tags", typeof(object))
            {
                Value = new BicepDictionary<string>()
            };

            infra.Add(tags);

            ProvisioningVariable? resourceToken = null;
            if (appEnvResource.UseAzdNamingConvention)
            {
                resourceToken = new ProvisioningVariable("resourceToken", typeof(string))
                {
                    Value = BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)
                };
                infra.Add(resourceToken);
            }

            var identity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{appEnvResource.Name}_mi"))
            {
                Tags = tags
            };

            infra.Add(identity);

            ContainerRegistryService? containerRegistry = null;
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (appEnvResource.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var registryReferenceAnnotation) && registryReferenceAnnotation.Registry is AzureProvisioningResource registry)
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            {
                containerRegistry = (ContainerRegistryService)registry.AddAsExistingResource(infra);
            }
            else
            {
                containerRegistry = new ContainerRegistryService(Infrastructure.NormalizeBicepIdentifier($"{appEnvResource.Name}_acr"))
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

            var laWorkspace = new OperationalInsightsWorkspace(Infrastructure.NormalizeBicepIdentifier($"{appEnvResource.Name}_law"))
            {
                Sku = new() { Name = OperationalInsightsWorkspaceSkuName.PerGB2018 },
                Tags = tags
            };

            infra.Add(laWorkspace);

            var containerAppEnvironment = new ContainerAppManagedEnvironment(appEnvResource.GetBicepIdentifier())
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

            var managedStorages = new Dictionary<string, ContainerAppManagedEnvironmentStorage>();

            var resource = (AzureContainerAppEnvironmentResource)infra.AspireResource;

            StorageAccount? storageVolume = null;
            if (resource.VolumeNames.Count > 0)
            {
                storageVolume = new StorageAccount(Infrastructure.NormalizeBicepIdentifier($"{appEnvResource.Name}_storageVolume"))
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

                    if (appEnvResource.UseAzdNamingConvention)
                    {
                        var volumeName = output.volume.Type switch
                        {
                            ContainerMountType.BindMount => $"bm{output.index}",
                            ContainerMountType.Volume => output.volume.Source ?? $"v{output.index}",
                            _ => throw new NotSupportedException()
                        };

                        // Remove '.' and '-' characters from volumeName
                        volumeName = volumeName.Replace(".", "").Replace("-", "");

                        share.Name = BicepFunction.Take(
                            BicepFunction.Interpolate(
                                $"{BicepFunction.ToLower(output.resource.Name)}-{BicepFunction.ToLower(volumeName)}"),
                            60);

                        containerAppStorage.Name = BicepFunction.Take(
                            BicepFunction.Interpolate(
                                $"{BicepFunction.ToLower(output.resource.Name)}-{BicepFunction.ToLower(volumeName)}"),
                            32);
                    }
                }
            }

            // Add the volume outputs to the container app environment storage
            foreach (var (key, value) in managedStorages)
            {
                infra.Add(new ProvisioningOutput(key, typeof(string))
                {
                    // use an expression here in case the resource's Name was set to a function expression above
                    Value = new MemberExpression(new IdentifierExpression(value.BicepIdentifier), "name")
                });
            }

            if (appEnvResource.UseAzdNamingConvention)
            {
                Debug.Assert(resourceToken is not null);

                identity.Name = BicepFunction.Interpolate($"mi-{resourceToken}");
                containerRegistry.Name = new FunctionCallExpression(
                    new IdentifierExpression("replace"),
                    new InterpolatedStringExpression([
                        new StringLiteralExpression("acr-"),
                        new IdentifierExpression(resourceToken.BicepIdentifier)
                    ]),
                    new StringLiteralExpression("-"),
                    new StringLiteralExpression(""));
                laWorkspace.Name = BicepFunction.Interpolate($"law-{resourceToken}");
                containerAppEnvironment.Name = BicepFunction.Interpolate($"cae-{resourceToken}");

#pragma warning disable IDE0031 // Use null propagation (IDE0031)
                if (storageVolume is not null)
#pragma warning restore IDE0031
                {
                    storageVolume.Name = BicepFunction.Interpolate($"vol{resourceToken}");
                }
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

    /// <summary>
    /// Configures the container app environment resources to use the same naming conventions as azd.
    /// </summary>
    /// <param name="builder">The AzureContainerAppEnvironmentResource to configure.</param>
    /// <returns><see cref="IResourceBuilder{T}"/></returns>
    /// <remarks>
    /// By default, the container app environment resources use a different naming convention than azd.
    ///
    /// This method allows for reusing the previously deployed resources if the application was deployed using
    /// azd without calling <see cref="AddAzureContainerAppEnvironment"/>
    /// </remarks>
    public static IResourceBuilder<AzureContainerAppEnvironmentResource> WithAzdResourceNaming(this IResourceBuilder<AzureContainerAppEnvironmentResource> builder)
    {
        builder.Resource.UseAzdNamingConvention = true;
        return builder;
    }
}
