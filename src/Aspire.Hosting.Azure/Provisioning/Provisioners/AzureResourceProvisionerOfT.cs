// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.Authorization;
using Azure;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed record UserPrincipal(Guid Id, string Name);

internal sealed class ProvisioningContext(
    TokenCredential credential,
    ArmClient armClient,
    SubscriptionResource subscription,
    ResourceGroupResource resourceGroup,
    TenantResource tenant,
    IReadOnlyDictionary<string, ArmResource> resourceMap,
    AzureLocation location,
    UserPrincipal principal,
    JsonObject userSecrets)
{
    public TokenCredential Credential => credential;
    public ArmClient ArmClient => armClient;
    public SubscriptionResource Subscription => subscription;
    public TenantResource Tenant => tenant;
    public ResourceGroupResource ResourceGroup => resourceGroup;
    public IReadOnlyDictionary<string, ArmResource> ResourceMap => resourceMap;
    public AzureLocation Location => location;
    public UserPrincipal Principal => principal;
    public JsonObject UserSecrets => userSecrets;
}

internal interface IAzureResourceProvisioner
{
    Task<bool> ConfigureResourceAsync(IConfiguration configuration, IAzureResource resource, CancellationToken cancellationToken);

    bool ShouldProvision(IConfiguration configuration, IAzureResource resource);

    Task GetOrCreateResourceAsync(
        IAzureResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken);
}

internal abstract class AzureResourceProvisioner<TResource> : IAzureResourceProvisioner
    where TResource : IAzureResource
{
    Task<bool> IAzureResourceProvisioner.ConfigureResourceAsync(IConfiguration configuration, IAzureResource resource, CancellationToken cancellationToken) =>
        ConfigureResourceAsync(configuration, (TResource)resource, cancellationToken);

    bool IAzureResourceProvisioner.ShouldProvision(IConfiguration configuration, IAzureResource resource) =>
        ShouldProvision(configuration, (TResource)resource);

    Task IAzureResourceProvisioner.GetOrCreateResourceAsync(
        IAzureResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
        => GetOrCreateResourceAsync((TResource)resource, context, cancellationToken);

    public abstract Task<bool> ConfigureResourceAsync(IConfiguration configuration, TResource resource, CancellationToken cancellationToken);

    public virtual bool ShouldProvision(IConfiguration configuration, TResource resource) => true;

    public abstract Task GetOrCreateResourceAsync(
        TResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken);

    protected static ResourceIdentifier CreateRoleDefinitionId(SubscriptionResource subscription, string roleDefinitionId) =>
        new($"{subscription.Id}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}");

    protected static async Task DoRoleAssignmentAsync(
        ArmClient armClient,
        ResourceIdentifier resourceId,
        Guid principalId,
        ResourceIdentifier roleDefinitionId,
        CancellationToken cancellationToken)
    {
        var roleAssignments = armClient.GetRoleAssignments(resourceId);
        await foreach (var ra in roleAssignments.GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (ra.Data.PrincipalId == principalId &&
                ra.Data.RoleDefinitionId.Equals(roleDefinitionId))
            {
                return;
            }
        }

        var roleAssignmentInfo = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

        var roleAssignmentId = Guid.NewGuid().ToString();
        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, roleAssignmentInfo, cancellationToken).ConfigureAwait(false);
    }
}
