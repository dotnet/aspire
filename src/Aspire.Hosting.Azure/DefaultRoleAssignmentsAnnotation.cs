// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
/// <param name="roleGuids"></param>
public class DefaultRoleAssignmentsAnnotation(IReadOnlyList<string> roleGuids) : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> RoleGuids { get; } = roleGuids;
}
