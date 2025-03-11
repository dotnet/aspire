// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the role assignments that have been applied to an Azure resource
/// and should be included in its Bicep template. These role assignments will grant the
/// <see cref="AzureBicepResource.KnownParameters.PrincipalId"/> permissions to the Azure resource.
/// </summary>
/// <param name="roles">The roles to generate in the resource's Bicep template.</param>
/// <remarks>
/// <see cref="AppliedRoleAssignmentsAnnotation"/> are used when the entire application utilizes a single managed identity.
/// For instance, during local development provisioning, all resources will use the same managed identity,
/// which is the currently logged-in developer.
/// 
/// When using provisioning infrastructure that supports targeted role assignments (e.g., AddAzureContainerAppsInfrastructure),
/// <see cref="AppliedRoleAssignmentsAnnotation"/> are not used.
/// </remarks>
public class AppliedRoleAssignmentsAnnotation(HashSet<RoleDefinition> roles) : IResourceAnnotation
{
    /// <summary>
    /// Gets the set of roles to generate in the resource's bicep template.
    /// </summary>
    public HashSet<RoleDefinition> Roles { get; } = roles;
}
