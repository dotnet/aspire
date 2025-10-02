// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.DeploymentState;

namespace Aspire.Hosting.Azure.Provisioning;

/// <summary>
/// Provides functionality for provisioning Azure Bicep resources.
/// </summary>
internal interface IBicepProvisioner
{
    /// <summary>
    /// Configures an Azure Bicep resource from deployment state.
    /// </summary>
    /// <param name="deploymentStateProvider">The deployment state provider containing Azure deployment settings.</param>
    /// <param name="resource">The Azure Bicep resource to configure.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a value indicating whether the resource was successfully configured.</returns>
    Task<bool> ConfigureResourceAsync(IDeploymentStateProvider deploymentStateProvider, AzureBicepResource resource, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an existing resource or creates a new Azure Bicep resource.
    /// </summary>
    /// <param name="resource">The Azure Bicep resource to get or create.</param>
    /// <param name="context">The provisioning context containing Azure subscription, resource group, and other deployment details.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken);
}
