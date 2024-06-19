// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Provisioning;

internal class ProvisioningContext(DistributedApplicationModel model)
{
    public DistributedApplicationModel AppModel { get; } = model;
}

internal interface IAWSResourceProvisioner
{
    bool ShouldProvision(IAWSResource resource);

    Task GetOrCreateResourceAsync(IAWSResource resource, CancellationToken cancellationToken = default);
}

internal abstract class AWSResourceProvisioner<TResource> : IAWSResourceProvisioner
    where TResource : IAWSResource
{
    Task IAWSResourceProvisioner.GetOrCreateResourceAsync(
        IAWSResource resource,
        CancellationToken cancellationToken)
        => GetOrCreateResourceAsync((TResource)resource, cancellationToken);

    public bool ShouldProvision(IAWSResource resource)
    {
        throw new NotImplementedException();
    }

    protected abstract Task GetOrCreateResourceAsync(TResource resource, CancellationToken cancellationToken);
}
