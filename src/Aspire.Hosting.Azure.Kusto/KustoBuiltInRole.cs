// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Provisioning.Kusto;

/// <summary>
/// Built-in Azure roles for Azure Data Explorer (Kusto) clusters.
/// </summary>
/// <remarks>
/// These roles correspond to the built-in Azure RBAC roles for managing Azure Data Explorer clusters.
/// For the most up-to-date information on roles, see the Azure documentation:
/// https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
/// </remarks>
public enum KustoBuiltInRole
{
    /// <summary>
    /// Grants full access to manage all resources, including the Kusto cluster, but does not allow you to assign roles in Azure RBAC.
    /// This is the standard Contributor role applied to Kusto resources.
    /// </summary>
    /// <remarks>
    /// Role Definition ID: b24988ac-6180-42a0-ab88-20f7382dd24c
    /// </remarks>
    Contributor,

    /// <summary>
    /// View all resources, but does not allow you to make any changes.
    /// This is the standard Reader role applied to Kusto resources.
    /// </summary>
    /// <remarks>
    /// Role Definition ID: acdd72a7-3385-48ef-bd42-f606fba81ae7
    /// </remarks>
    Reader
}

/// <summary>
/// Extension methods for <see cref="KustoBuiltInRole"/>.
/// </summary>
public static class KustoBuiltInRoleExtensions
{
    /// <summary>
    /// Gets the role definition ID for the specified built-in role.
    /// </summary>
    /// <param name="role">The built-in role.</param>
    /// <returns>The Azure role definition ID (GUID).</returns>
    public static string GetBuiltInRoleName(this KustoBuiltInRole role)
    {
        return role switch
        {
            KustoBuiltInRole.Contributor => "b24988ac-6180-42a0-ab88-20f7382dd24c",
            KustoBuiltInRole.Reader => "acdd72a7-3385-48ef-bd42-f606fba81ae7",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown built-in role.")
        };
    }
}