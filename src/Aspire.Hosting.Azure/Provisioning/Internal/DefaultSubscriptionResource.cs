// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Core;
using Azure.ResourceManager;
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

    private sealed class DefaultSubscriptionData(SubscriptionData subscriptionData) : ISubscriptionData
    {
        public ResourceIdentifier Id => subscriptionData.Id;
        public string? DisplayName => subscriptionData.DisplayName;
        public Guid? TenantId => subscriptionData.TenantId;
    }

    private sealed class DefaultResourceGroupCollection(ResourceGroupCollection resourceGroupCollection) : IResourceGroupCollection
    {
        public async Task<Response<IResourceGroupResource>> GetAsync(string resourceGroupName, CancellationToken cancellationToken = default)
        {
            var response = await resourceGroupCollection.GetAsync(resourceGroupName, cancellationToken).ConfigureAwait(false);
            return Response.FromValue<IResourceGroupResource>(new DefaultResourceGroupResource(response.Value), response.GetRawResponse());
        }

        public async Task<ArmOperation<IResourceGroupResource>> CreateOrUpdateAsync(WaitUntil waitUntil, string resourceGroupName, ResourceGroupData data, CancellationToken cancellationToken = default)
        {
            var operation = await resourceGroupCollection.CreateOrUpdateAsync(waitUntil, resourceGroupName, data, cancellationToken).ConfigureAwait(false);
            var wrappedValue = new DefaultResourceGroupResource(operation.Value);

            // Create a wrapper for the ArmOperation that exposes the wrapped value
            return new DefaultArmOperation<IResourceGroupResource>(operation, wrappedValue);
        }
    }
}
