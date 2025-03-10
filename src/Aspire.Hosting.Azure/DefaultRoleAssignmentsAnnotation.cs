// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Specifies the default role assignments to be applied to an Azure resource
/// when no specific role assignments (i.e., <see cref="RoleAssignmentAnnotation"/>) are provided.
/// </summary>
/// <param name="roles">The default set of roles for an Azure resource.</param>
public class DefaultRoleAssignmentsAnnotation(IReadOnlySet<RoleDefinition> roles) : IResourceAnnotation
{
    /// <summary>
    /// Gets the set of roles to use by default for an Azure resource.
    /// </summary>
    public IReadOnlySet<RoleDefinition> Roles { get; } = roles;
}
