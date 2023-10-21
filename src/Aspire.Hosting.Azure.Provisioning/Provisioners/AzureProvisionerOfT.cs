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

namespace Aspire.Hosting.Azure.Provisioning;

internal interface IAzuresourceProvisioner
{
    bool ConfigureResource(IConfiguration configuration, IAzureResource resource);

    Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        IAzureResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken);
}

internal abstract class AzureResourceProvisioner<TResource> : IAzuresourceProvisioner
    where TResource : IAzureResource
{
    bool IAzuresourceProvisioner.ConfigureResource(IConfiguration configuration, IAzureResource resource) =>
        ConfigureResource(configuration, (TResource)resource);

    Task IAzuresourceProvisioner.GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        IAzureResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
        => GetOrCreateResourceAsync(armClient, subscription, resourceGroup, resourceMap, location, (TResource)resource, principalId, userSecrets, cancellationToken);

    public abstract bool ConfigureResource(IConfiguration configuration, TResource resource);

    public abstract Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        TResource resource,
        Guid principalId,
        JsonObject userSecrets,
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
        await foreach (var ra in roleAssignments.GetAllAsync(cancellationToken: cancellationToken))
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
