// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Authorization;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Context for adding role assignments to an Azure resource.
/// </summary>
public sealed class AddRoleAssignmentsContext
{
    private readonly Lazy<BicepValue<RoleManagementPrincipalType>> _getPrincipalType;
    private readonly Lazy<BicepValue<Guid>> _getPrincipalId;
    private readonly Lazy<BicepValue<string>> _getPrincipalName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddRoleAssignmentsContext"/> class.
    /// </summary>
    /// <param name="infrastructure">The Azure resource infrastructure to add the role assignments into.</param>
    /// <param name="roles">The roles to be assigned.</param>
    /// <param name="getPrincipalType">A Lazy instance that will retrieve the principal type when requested.</param>
    /// <param name="getPrincipalId">A Lazy instance that will retrieve the principal ID when requested.</param>
    /// <param name="getPrincipalName">A Lazy instance that will retrieve the principal name when requested.</param>
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
    /// Gets the Azure resource infrastructure to add the role assignments into.
    /// </summary>
    public AzureResourceInfrastructure Infrastructure { get; }

    /// <summary>
    /// Gets the roles to be assigned.
    /// </summary>
    public IEnumerable<RoleDefinition> Roles { get; }

    /// <summary>
    /// Gets the principal type to use in the role assignment.
    /// </summary>
    public BicepValue<RoleManagementPrincipalType> PrincipalType => _getPrincipalType.Value;

    /// <summary>
    /// Gets the principal ID to use in the role assignment.
    /// </summary>
    public BicepValue<Guid> PrincipalId => _getPrincipalId.Value;

    /// <summary>
    /// Gets the principal name to use in the role assignment.
    /// </summary>
    public BicepValue<string> PrincipalName => _getPrincipalName.Value;
}
