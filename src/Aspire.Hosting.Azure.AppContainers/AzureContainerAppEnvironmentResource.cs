// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents an Azure Container App Environment resource.
/// </summary>
/// <param name="name">The name of the Container App Environment.</param>
/// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
public class AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure), IAzureContainerAppEnvironment
{
    /// <summary>
    /// Gets the unique identifier of the Container App Environment.
    /// </summary>
    public BicepOutputReference ContainerAppEnvironmentId => new("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", this);

    /// <summary>
    /// Gets the default domain associated with the Container App Environment.
    /// </summary>
    public BicepOutputReference ContainerAppDomain => new("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", this);

    /// <summary>
    /// Gets the URL endpoint of the associated Azure Container Registry.
    /// </summary>
    public BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);

    /// <summary>
    /// Gets the managed identity ID associated with the Azure Container Registry.
    /// </summary>
    public BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    /// <summary>
    /// Gets the unique identifier of the Log Analytics workspace.
    /// </summary>
    public BicepOutputReference LogAnalyticsWorkspaceId => new("AZURE_LOG_ANALYTICS_WORKSPACE_ID", this);

    /// <summary>
    /// Gets the principal name of the managed identity.
    /// </summary>
    public BicepOutputReference PrincipalName => new("MANAGED_IDENTITY_NAME", this);

    /// <summary>
    /// Gets the principal ID of the managed identity.
    /// </summary>
    public BicepOutputReference PrincipalId => new("MANAGED_IDENTITY_PRINCIPAL_ID", this);

    /// <summary>
    /// Gets the name of the Container App Environment.
    /// </summary>
    public BicepOutputReference ContainerAppEnvironmentName => new("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME", this);

    internal Dictionary<string, BicepOutputReference> VolumeNames { get; } = [];

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppEnvironmentId => ContainerAppEnvironmentId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppDomain => ContainerAppDomain;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryUrl => ContainerRegistryUrl;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryManagedIdentityId => ContainerRegistryManagedIdentityId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.LogAnalyticsWorkspaceId => LogAnalyticsWorkspaceId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.PrincipalId => PrincipalId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.PrincipalName => PrincipalName;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppEnvironmentName => ContainerAppEnvironmentName;

    IManifestExpressionProvider IAzureContainerAppEnvironment.GetSecretOutputKeyVault(AzureBicepResource resource)
    {
        throw new NotSupportedException("Automatic Key vault generation is not supported in this environment. Please create a key vault resource directly.");
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
