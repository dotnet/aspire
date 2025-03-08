// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Specifies the roles that the current resource should be assigned to the target resource.
/// </summary>
/// <param name="target">The Azure resource that the current resource will interact with.</param>
/// <param name="roles">The roles that the current resource should be assigned to <paramref name="target"/>.</param>
public class RoleAssignmentAnnotation(AzureProvisioningResource target, IReadOnlySet<RoleDefinition> roles) : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public AzureProvisioningResource Target { get; } = target;

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlySet<RoleDefinition> Roles { get; } = roles;
}
