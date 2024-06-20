// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Provisioning;

internal class ProvisioningContext(ILookup<IAWSResource?, IResourceWithParent>? parentChildLookup)
{
    public ILookup<IAWSResource?, IResourceWithParent>? ParentChildLookup { get; } = parentChildLookup;
}

internal interface IAWSResourceProvisioner
{
    bool ShouldProvision(IAWSResource resource);

    Task GetOrCreateResourceAsync(IAWSResource resource, ProvisioningContext context, CancellationToken cancellationToken = default);
}

internal abstract class AWSResourceProvisioner<TResource> : IAWSResourceProvisioner
    where TResource : IAWSResource
{
    Task IAWSResourceProvisioner.GetOrCreateResourceAsync(
        IAWSResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
        => GetOrCreateResourceAsync((TResource)resource, context, cancellationToken);

    public virtual bool ShouldProvision(IAWSResource resource) => true;

    protected abstract Task GetOrCreateResourceAsync(TResource resource, ProvisioningContext context, CancellationToken cancellationToken);
}
