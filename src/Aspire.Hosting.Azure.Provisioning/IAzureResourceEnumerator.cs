// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning;

internal interface IAzureResourceEnumerator
{
    IAsyncEnumerable<ArmResource> GetResources(ResourceGroupResource resourceGroup);
    IDictionary<string, string> GetTags(ArmResource resource);
}

internal sealed class AzureResourceEnumerator<TResource>(
    Func<ResourceGroupResource, IAsyncEnumerable<TResource>> getResources,
    Func<TResource, IDictionary<string, string>> getTags) : IAzureResourceEnumerator
    where TResource : ArmResource
{
    public IAsyncEnumerable<TResource> GetResources(ResourceGroupResource resourceGroup) => getResources(resourceGroup);

    public IDictionary<string, string> GetTags(TResource resource) => getTags(resource);

    async IAsyncEnumerable<ArmResource> IAzureResourceEnumerator.GetResources(ResourceGroupResource resourceGroup)
    {
        await foreach (var resource in GetResources(resourceGroup))
        {
            yield return resource;
        }
    }

    IDictionary<string, string> IAzureResourceEnumerator.GetTags(ArmResource resource)
    {
        return GetTags((TResource)resource);
    }
}
