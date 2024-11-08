// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
/// <param name="target"></param>
/// <param name="roleGuids"></param>
public class RoleAssignmentAnnotation(IAzureResource target, IEnumerable<string> roleGuids) : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public IAzureResource Target { get; } = target;

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> RoleGuids { get; } = roleGuids;
}
