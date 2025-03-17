// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// 
/// </summary>
/// <param name="name"></param>
/// <param name="configureInfrastructure"></param>
public class AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure), IAzureContainerAppEnvironment
{
    /// <summary>
    /// 
    /// </summary>
    public BicepOutputReference ContainerAppEnvironmentId => new("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", this);

    /// <summary>
    /// 
    /// </summary>
    public BicepOutputReference ContainerAppDomain => new("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", this);

    /// <summary>
    /// 
    /// </summary>
    public BicepOutputReference ManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    /// <summary>
    /// 
    /// </summary>
    public BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);

    /// <summary>
    /// 
    /// </summary>
    public BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    internal Dictionary<string, BicepOutputReference> VolumeNames { get; } = [];

    internal Dictionary<string, BicepOutputReference> SecretKeyVaultNames { get; } = [];

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppEnvironmentId => ContainerAppEnvironmentId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppDomain => ContainerAppDomain;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryUrl => ContainerRegistryUrl;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryManagedIdentityId => ContainerRegistryManagedIdentityId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ManagedIdentityId => ManagedIdentityId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.GetSecretOutputKeyVault(AzureBicepResource resource)
    {
        // REVIEW: Should we use the same naming algorithm as azd?
        var outputName = $"secret_output_{resource.Name}";

        if (!SecretKeyVaultNames.TryGetValue(outputName, out var outputReference))
        {
            outputReference = new BicepOutputReference(outputName, this);

            SecretKeyVaultNames[outputName] = outputReference;
        }

        return outputReference;
    }

    IManifestExpressionProvider IAzureContainerAppEnvironment.GetVolumeStorage(IResource resource, ContainerMountType type, string volumeIndex)
    {
        // REVIEW: Should we use the same naming algorithm as azd?
        var outputName = $"volumes_{resource.Name}_{volumeIndex}";

        if (!VolumeNames.TryGetValue(outputName, out var outputReference))
        {
            outputReference = new BicepOutputReference(outputName, this);

            VolumeNames[outputName] = outputReference;
        }

        return outputReference;
    }
}
