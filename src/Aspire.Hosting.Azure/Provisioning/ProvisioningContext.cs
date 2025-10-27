// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure.Core;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed record UserPrincipal(Guid Id, string Name);

internal sealed class ProvisioningContext(
    TokenCredential credential,
    IArmClient armClient,
    ISubscriptionResource subscription,
    IResourceGroupResource resourceGroup,
    ITenantResource tenant,
    AzureLocation location,
    UserPrincipal principal,
    DistributedApplicationExecutionContext executionContext)
{
    public TokenCredential Credential => credential;
    public IArmClient ArmClient => armClient;
    public ISubscriptionResource Subscription => subscription;
    public ITenantResource Tenant => tenant;
    public IResourceGroupResource ResourceGroup => resourceGroup;
    public AzureLocation Location => location;
    public UserPrincipal Principal => principal;
    public DistributedApplicationExecutionContext ExecutionContext => executionContext;
}
