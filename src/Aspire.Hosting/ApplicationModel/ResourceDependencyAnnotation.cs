// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates a resource depends on another resource.
/// </summary>
/// <param name="resource">The dependency</param>
public class ResourceDependencyAnnotation(IResource resource) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;
}
