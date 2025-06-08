// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="ISubscriptionResource"/>.
/// </summary>
internal sealed class DefaultSubscriptionResource(SubscriptionResource subscriptionResource) : ISubscriptionResource
{
    public ResourceIdentifier Id => subscriptionResource.Id;
    public ISubscriptionData Data { get; } = new DefaultSubscriptionData(subscriptionResource.Data);

    public async Task<IResourceGroupResource> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var resourceGroup = await subscriptionResource.GetResourceGroupAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
        return new DefaultResourceGroupResource(resourceGroup.Value);
    }

    public IResourceGroupCollection GetResourceGroups()
    {
        return new DefaultResourceGroupCollection(subscriptionResource.GetResourceGroups());
    }
}