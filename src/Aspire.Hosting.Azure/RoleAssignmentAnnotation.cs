// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
/// <param name="target"></param>
/// <param name="roles"></param>
public class RoleAssignmentAnnotation(AzureProvisioningResource target, IReadOnlyList<(string, string)> roles) : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public AzureProvisioningResource Target { get; } = target;

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<(string Id, string Description)> Roles { get; } = roles;
}
