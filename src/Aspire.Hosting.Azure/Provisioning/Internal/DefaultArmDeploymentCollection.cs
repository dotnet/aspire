// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IArmDeploymentCollection"/>.
/// </summary>
internal sealed class DefaultArmDeploymentCollection(ArmDeploymentCollection armDeploymentCollection) : IArmDeploymentCollection
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