// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents an Azure Container App Environment resource.
/// </summary>
/// <param name="name">The name of the Container App Environment.</param>
/// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
public class AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    AzureProvisioningResource(name, configureInfrastructure), IComputeEnvironmentResource, IAzureContainerRegistry
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    internal bool UseAzdNamingConvention { get; set; }

    /// <summary>
    /// Gets the unique identifier of the Container App Environment.
    /// </summary>
    internal BicepOutputReference ContainerAppEnvironmentId => new("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", this);

    /// <summary>
    /// Gets the default domain associated with the Container App Environment.
    /// </summary>
    internal BicepOutputReference ContainerAppDomain => new("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", this);

    /// <summary>
    /// Gets the URL endpoint of the associated Azure Container Registry.
    /// </summary>
    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);

    /// <summary>
    /// Gets the managed identity ID associated with the Azure Container Registry.
    /// </summary>
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    /// <summary>
    /// Gets the name of the Container App Environment.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME", this);

    /// <summary>
    /// Gets the container registry name.
    /// </summary>
    private BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);

    internal Dictionary<string, (IResource resource, ContainerMountAnnotation volume, int index, BicepOutputReference outputReference)> VolumeNames { get; } = [];

    // Implement IAzureContainerRegistry interface
    ReferenceExpression IContainerRegistry.Name => ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint => ReferenceExpression.Create($"{ContainerRegistryUrl}");

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId => ReferenceExpression.Create($"{ContainerRegistryManagedIdentityId}");

    internal BicepOutputReference GetVolumeStorage(IResource resource, ContainerMountAnnotation volume, int volumeIndex)
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

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        // Even though it's a compound resource, we'll only expose the managed environment
        var cae = ContainerAppManagedEnvironment.FromExisting(this.GetBicepIdentifier());
        cae.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(cae);
        return cae;
    }
}
