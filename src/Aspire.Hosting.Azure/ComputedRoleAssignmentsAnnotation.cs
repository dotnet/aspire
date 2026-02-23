// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation that tracks the role assignment resources that were created for a resource during provisioning.
/// </summary>
public sealed class ComputedRoleAssignmentsAnnotation(IReadOnlyList<AzureBicepResource> roleAssignmentResources) : IResourceAnnotation
{
    /// <summary>
    /// The role assignment resources created for the resource.
    /// </summary>
    public IReadOnlyList<AzureBicepResource> RoleAssignmentResources { get; } = roleAssignmentResources;
}
