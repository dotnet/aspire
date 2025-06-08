// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IResourceGroupResource"/>.
/// </summary>
internal sealed class DefaultResourceGroupResource(ResourceGroupResource resourceGroupResource) : IResourceGroupResource
{
    public ResourceIdentifier Id => resourceGroupResource.Id;
    public IResourceGroupData Data { get; } = new DefaultResourceGroupData(resourceGroupResource.Data);

    public IArmDeploymentCollection GetArmDeployments()
    {
        return new DefaultArmDeploymentCollection(resourceGroupResource.GetArmDeployments());
    }

    private sealed class DefaultResourceGroupData(ResourceGroupData resourceGroupData) : IResourceGroupData
    {
        public string Name => resourceGroupData.Name;
    }

    private sealed class DefaultArmDeploymentCollection(ArmDeploymentCollection armDeploymentCollection) : IArmDeploymentCollection
    {
        public Task<ArmOperation<ArmDeploymentResource>> CreateOrUpdateAsync(
            WaitUntil waitUntil,
            string deploymentName,
            ArmDeploymentContent content,
            CancellationToken cancellationToken = default)
        {
            return armDeploymentCollection.CreateOrUpdateAsync(waitUntil, deploymentName, content, cancellationToken);
        }
    }
}
