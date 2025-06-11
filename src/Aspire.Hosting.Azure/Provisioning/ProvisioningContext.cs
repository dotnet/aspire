// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed record UserPrincipal(Guid Id, string Name);

internal sealed class ProvisioningContext(
    TokenCredential credential,
    ArmClient armClient,
    SubscriptionResource subscription,
    ResourceGroupResource resourceGroup,
    TenantResource tenant,
    AzureLocation location,
    UserPrincipal principal,
    JsonObject userSecrets)
{
    public TokenCredential Credential => credential;
    public ArmClient ArmClient => armClient;
    public SubscriptionResource Subscription => subscription;
    public TenantResource Tenant => tenant;
    public ResourceGroupResource ResourceGroup => resourceGroup;
    public AzureLocation Location => location;
    public UserPrincipal Principal => principal;
    public JsonObject UserSecrets => userSecrets;
}