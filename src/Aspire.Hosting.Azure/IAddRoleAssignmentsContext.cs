// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Authorization;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Context for adding role assignments to an Azure resource.
/// </summary>
public interface IAddRoleAssignmentsContext
{
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
    public BicepValue<RoleManagementPrincipalType> PrincipalType { get; }

    /// <summary>
    /// Gets the principal ID to use in the role assignment.
    /// </summary>
    public BicepValue<Guid> PrincipalId { get; }

    /// <summary>
    /// Gets the principal name to use in the role assignment.
    /// </summary>
    /// <remarks>
    /// Not all role assignments require/use a principal name.
    /// </remarks>
    public BicepValue<string> PrincipalName { get; }
}
