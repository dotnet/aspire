// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a wait relationship between two resources.
/// </summary>
/// <param name="resource">The resource that will be waited on.</param>
/// <param name="waitType">The type of wait to apply to the dependency resource.</param>
/// <param name="exitCode">The exit code that the resource must return for the wait to be satisfied.</param>
/// <remarks>
/// The holder of this annotation is waiting on the resource in the <see cref="WaitAnnotation.Resource"/> property.
/// </remarks>
[DebuggerDisplay("Resource = {Resource.Name}")]
public class WaitAnnotation(IResource resource, WaitType waitType, int exitCode = 0) : IResourceAnnotation
{
    /// <summary>
    /// The resource that will be waited on.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The type of wait to apply to the dependency resource.
    /// </summary>
    public WaitType WaitType { get; } = waitType;

    /// <summary>
    /// The exit code that the resource must return for the wait to be satisfied.
    /// </summary>
    public int ExitCode { get; } = exitCode;
}

/// <summary>
/// Specifies the type of Wait applied to dependency resources.
/// </summary>
public enum WaitType
{
    /// <summary>
    /// Dependent resource will wait until resource starts and all health checks are satisfied.
    /// </summary>
    WaitUntilHealthy,

    /// <summary>
    /// Dependent resource will wait until resource completes.
    /// </summary>
    WaitForCompletion
}
