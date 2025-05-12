#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.ContainerRegistry;

/// <summary>
/// Represents an Azure Container Registry resource.
/// </summary>
public class AzureContainerRegistryResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IContainerRegistry
{
    /// <summary>
    /// The name of the Azure Container Registry.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// The endpoint of the Azure Container Registry.
    /// </summary>
    public BicepOutputReference RegistryEndpoint => new("loginServer", this);

    /// <inheritdoc/>
    ReferenceExpression IContainerRegistry.Name => ReferenceExpression.Create($"{NameOutputReference}");

    /// <inheritdoc/>
    ReferenceExpression IContainerRegistry.Endpoint => ReferenceExpression.Create($"{RegistryEndpoint}");

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = ContainerRegistryService.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }
}
