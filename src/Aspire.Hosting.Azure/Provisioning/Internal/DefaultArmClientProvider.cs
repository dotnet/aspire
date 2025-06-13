// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IArmClientProvider"/>.
/// </summary>
internal sealed class DefaultArmClientProvider : IArmClientProvider
{
    public IArmClient GetArmClient(TokenCredential credential, string subscriptionId)
    {
        var armClient = new ArmClient(credential, subscriptionId);
        return new DefaultArmClient(armClient);
    }

    private sealed class DefaultArmClient(ArmClient armClient) : IArmClient
    {
        public async Task<(ISubscriptionResource subscription, ITenantResource tenant)> GetSubscriptionAndTenantAsync(CancellationToken cancellationToken = default)
        {
            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);
            var subscriptionResource = new DefaultSubscriptionResource(subscription);

            ITenantResource? tenantResource = null;

            await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (tenant.Data.TenantId == subscriptionResource.TenantId)
                {
                    tenantResource = new DefaultTenantResource(tenant);
                    break;
                }
            }

            if (tenantResource is null)
            {
                throw new InvalidOperationException($"Could not find tenant id {subscriptionResource.TenantId} for subscription {subscriptionResource.DisplayName}.");
            }

            return (subscriptionResource, tenantResource);
        }

        private sealed class DefaultTenantResource(TenantResource tenantResource) : ITenantResource
        {
            public Guid? TenantId => tenantResource.Data.TenantId;
            public string? DefaultDomain => tenantResource.Data.DefaultDomain;
        }
    }
}
