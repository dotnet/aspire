// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
        public async Task<ISubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default)
        {
            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken).ConfigureAwait(false);
            return new DefaultSubscriptionResource(subscription);
        }

        public async IAsyncEnumerable<ITenantResource> GetTenantsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var tenant in armClient.GetTenants().GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                yield return new DefaultTenantResource(tenant);
            }
        }

        private sealed class DefaultTenantResource(TenantResource tenantResource) : ITenantResource
        {
            public ITenantData Data { get; } = new DefaultTenantData(tenantResource.Data);

            private sealed class DefaultTenantData(TenantData tenantData) : ITenantData
            {
                public Guid? TenantId => tenantData.TenantId;
                public string? DefaultDomain => tenantData.DefaultDomain;
            }
        }
    }
}
