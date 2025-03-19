// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Authorization;

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
public sealed class AddRoleAssignmentsContext
{
    private readonly Lazy<BicepValue<RoleManagementPrincipalType>> _getPrincipalType;
    private readonly Lazy<BicepValue<Guid>> _getPrincipalId;
    private readonly Lazy<BicepValue<string>> _getPrincipalName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="infrastructure"></param>
    /// <param name="roles"></param>
    /// <param name="getPrincipalType"></param>
    /// <param name="getPrincipalId"></param>
    /// <param name="getPrincipalName"></param>
    public AddRoleAssignmentsContext(
        AzureResourceInfrastructure infrastructure,
        IEnumerable<RoleDefinition> roles,
        Lazy<BicepValue<RoleManagementPrincipalType>> getPrincipalType,
        Lazy<BicepValue<Guid>> getPrincipalId,
        Lazy<BicepValue<string>> getPrincipalName)
    {
        Infrastructure = infrastructure ?? throw new ArgumentNullException(nameof(infrastructure));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));

        _getPrincipalType = getPrincipalType;
        _getPrincipalId = getPrincipalId;
        _getPrincipalName = getPrincipalName;
    }

    /// <summary>
    /// 
    /// </summary>
    public AzureResourceInfrastructure Infrastructure { get; }

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<RoleDefinition> Roles { get; }

    /// <summary>
    /// 
    /// </summary>
    public BicepValue<RoleManagementPrincipalType> PrincipalType => _getPrincipalType.Value;

    /// <summary>
    /// 
    /// </summary>
    public BicepValue<Guid> PrincipalId => _getPrincipalId.Value;

    /// <summary>
    /// 
    /// </summary>
    public BicepValue<string> PrincipalName => _getPrincipalName.Value;
}
