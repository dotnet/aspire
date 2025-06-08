// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.ResourceManager;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IArmClient"/>.
/// </summary>
internal sealed class DefaultArmClient(ArmClient armClient) : IArmClient
{
    public async Task<ISubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);
        return new DefaultSubscriptionResource(subscription);
    }

    public async IAsyncEnumerable<ITenantResource> GetTenantsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            yield return new DefaultTenantResource(tenant);
        }
    }
}