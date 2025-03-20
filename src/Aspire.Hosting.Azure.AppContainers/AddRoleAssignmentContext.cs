// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Authorization;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Context for adding role assignments to an Azure resource.
/// </summary>
internal sealed class AddRoleAssignmentsContext(
    AzureResourceInfrastructure infrastructure,
    IEnumerable<RoleDefinition> roles,
    Lazy<BicepValue<RoleManagementPrincipalType>> getPrincipalType,
    Lazy<BicepValue<Guid>> getPrincipalId,
    Lazy<BicepValue<string>> getPrincipalName) : IAddRoleAssignmentsContext
{
    public AzureResourceInfrastructure Infrastructure { get; } = infrastructure;

    public IEnumerable<RoleDefinition> Roles { get; } = roles;

    public BicepValue<RoleManagementPrincipalType> PrincipalType => getPrincipalType.Value;

    public BicepValue<Guid> PrincipalId => getPrincipalId.Value;

    public BicepValue<string> PrincipalName => getPrincipalName.Value;
}
