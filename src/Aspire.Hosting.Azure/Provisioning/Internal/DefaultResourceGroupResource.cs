// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager.Resources;

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
}