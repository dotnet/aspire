// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Amazon;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed record UserPrincipal(Guid Id, string Name);

internal sealed class ProvisioningContext(RegionEndpoint location)
{
    public RegionEndpoint Location => location;
}

internal interface IAwsResourceProvisioner
{
    bool ConfigureResource(IConfiguration configuration, IAwsResource resource);

    bool ShouldProvision(IConfiguration configuration, IAwsResource resource);

    Task GetOrCreateResourceAsync(
        IAwsResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken);
}

internal abstract class AwsResourceProvisioner<TResource> : IAwsResourceProvisioner
    where TResource : IAwsResource
{
    bool IAwsResourceProvisioner.ConfigureResource(IConfiguration configuration, IAwsResource resource) =>
        ConfigureResource(configuration, (TResource)resource);

    bool IAwsResourceProvisioner.ShouldProvision(IConfiguration configuration, IAwsResource resource) =>
        ShouldProvision(configuration, (TResource)resource);

    Task IAwsResourceProvisioner.GetOrCreateResourceAsync(
        IAwsResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken)
        => GetOrCreateResourceAsync((TResource)resource, context, cancellationToken);

    public abstract bool ConfigureResource(IConfiguration configuration, TResource resource);

    public virtual bool ShouldProvision(IConfiguration configuration, TResource resource) => true;

    public abstract Task GetOrCreateResourceAsync(
        TResource resource,
        ProvisioningContext context,
        CancellationToken cancellationToken);

    //protected static ResourceIdentifier CreateRoleDefinitionId(SubscriptionResource subscription, string roleDefinitionId) =>
    //    new($"{subscription.Id}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}");

    //protected static async Task DoRoleAssignmentAsync(
    //    ArmClient armClient,
    //    ResourceIdentifier resourceId,
    //    Guid principalId,
    //    ResourceIdentifier roleDefinitionId,
    //    CancellationToken cancellationToken)
    //{
    //    var roleAssignments = armClient.GetRoleAssignments(resourceId);
    //    await foreach (var ra in roleAssignments.GetAllAsync(cancellationToken: cancellationToken))
    //    {
    //        if (ra.Data.PrincipalId == principalId &&
    //            ra.Data.RoleDefinitionId.Equals(roleDefinitionId))
    //        {
    //            return;
    //        }
    //    }

    //    var roleAssignmentInfo = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId);

    //    var roleAssignmentId = Guid.NewGuid().ToString();
    //    await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, roleAssignmentId, roleAssignmentInfo, cancellationToken).ConfigureAwait(false);
    //}
}
