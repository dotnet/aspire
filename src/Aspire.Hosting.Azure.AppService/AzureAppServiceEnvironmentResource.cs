// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure App Service Environment resource.
/// </summary>
/// <param name="name">The name of the Azure App Service Environment.</param>
/// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
public class AzureAppServiceEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    IAzureComputeEnvironmentResource,
    IAzureContainerRegistry
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    // We don't want these to be public if we end up with an app service
    // per compute resource.
    internal BicepOutputReference PlanIdOutputReference => new("planId", this);
    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);
    internal BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);
    internal BicepOutputReference ContainerRegistryClientId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", this);

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId => 
        ReferenceExpression.Create($"{ContainerRegistryManagedIdentityId}");

    ReferenceExpression IContainerRegistry.Name => 
        ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint => 
        ReferenceExpression.Create($"{ContainerRegistryUrl}");
}
