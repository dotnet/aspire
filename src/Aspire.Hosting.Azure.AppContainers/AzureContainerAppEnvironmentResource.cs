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
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    AzureProvisioningResource(name, configureInfrastructure), IComputeEnvironmentResource, IAzureContainerAppEnvironment, IAzureContainerRegistry
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    internal bool UseAzdNamingConvention { get; set; }

    /// <summary>
    /// Gets the unique identifier of the Container App Environment.
    /// </summary>
    private BicepOutputReference ContainerAppEnvironmentId => new("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", this);

    /// <summary>
    /// Gets the default domain associated with the Container App Environment.
    /// </summary>
    private BicepOutputReference ContainerAppDomain => new("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", this);

    /// <summary>
    /// Gets the URL endpoint of the associated Azure Container Registry.
    /// </summary>
    private BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);

    /// <summary>
    /// Gets the managed identity ID associated with the Azure Container Registry.
    /// </summary>
    private BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    /// <summary>
    /// Gets the unique identifier of the Log Analytics workspace.
    /// </summary>
    private BicepOutputReference LogAnalyticsWorkspaceId => new("AZURE_LOG_ANALYTICS_WORKSPACE_ID", this);

    /// <summary>
    /// Gets the principal name of the managed identity.
    /// </summary>
    private BicepOutputReference PrincipalName => new("MANAGED_IDENTITY_NAME", this);

    /// <summary>
    /// Gets the principal ID of the managed identity.
    /// </summary>
    private BicepOutputReference PrincipalId => new("MANAGED_IDENTITY_PRINCIPAL_ID", this);

    /// <summary>
    /// Gets the name of the Container App Environment.
    /// </summary>
    private BicepOutputReference ContainerAppEnvironmentName => new("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME", this);

    /// <summary>
    /// Gets the container registry name.
    /// </summary>
    private BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);

    internal Dictionary<string, (IResource resource, ContainerMountAnnotation volume, int index, BicepOutputReference outputReference)> VolumeNames { get; } = [];

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppEnvironmentId => ContainerAppEnvironmentId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppDomain => ContainerAppDomain;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryUrl => ContainerRegistryUrl;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerRegistryManagedIdentityId => ContainerRegistryManagedIdentityId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.LogAnalyticsWorkspaceId => LogAnalyticsWorkspaceId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.PrincipalId => PrincipalId;

    IManifestExpressionProvider IAzureContainerAppEnvironment.PrincipalName => PrincipalName;

    IManifestExpressionProvider IAzureContainerAppEnvironment.ContainerAppEnvironmentName => ContainerAppEnvironmentName;

    // Implement IAzureContainerRegistry interface
    ReferenceExpression IContainerRegistry.Name => ReferenceExpression.Create(ContainerRegistryName);

    ReferenceExpression IContainerRegistry.Endpoint => ReferenceExpression.Create(ContainerRegistryUrl);

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId => ReferenceExpression.Create(ContainerRegistryManagedIdentityId);

    IManifestExpressionProvider IAzureContainerAppEnvironment.GetSecretOutputKeyVault(AzureBicepResource resource)
    {
        throw new NotSupportedException("Automatic Key vault generation is not supported in this environment. Please create a key vault resource directly.");
    }

    IManifestExpressionProvider IAzureContainerAppEnvironment.GetVolumeStorage(IResource resource, ContainerMountAnnotation volume, int volumeIndex)
    {
        var prefix = volume.Type switch
        {
            ContainerMountType.BindMount => "bindmounts",
            ContainerMountType.Volume => "volumes",
            _ => throw new NotSupportedException()
        };

        // REVIEW: Should we use the same naming algorithm as azd?
        var outputName = $"{prefix}_{resource.Name}_{volumeIndex}";

        if (!VolumeNames.TryGetValue(outputName, out var volumeName))
        {
            volumeName = (resource, volume, volumeIndex, new BicepOutputReference(outputName, this));

            VolumeNames[outputName] = volumeName;
        }

        return volumeName.outputReference;
    }
}
