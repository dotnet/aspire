// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// The set of role assignments that have been applied to an Azure resource
/// and should be generated in its bicep template.
/// </summary>
/// <param name="roles">The list of role assignments to generate in the resource's bicep template.</param>
public class AppliedRoleAssignmentsAnnotation(HashSet<RoleDefinition> roles) : IResourceAnnotation
{
    /// <summary>
    /// Gets the list of role assignments to generate in the resource's bicep template.
    /// </summary>
    public HashSet<RoleDefinition> Roles { get; } = roles;
}
