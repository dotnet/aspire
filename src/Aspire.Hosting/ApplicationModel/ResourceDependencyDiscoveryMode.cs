// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies how resource dependencies are discovered.
/// </summary>
[Flags]
public enum ResourceDependencyDiscoveryMode
{
    /// <summary>
    /// Discover the full transitive closure of all dependencies.
    /// This includes direct dependencies and all dependencies of those dependencies, recursively.
    /// </summary>
    Recursive = 0,

    /// <summary>
    /// Discover only direct dependencies.
    /// This includes dependencies from annotations (parent, wait, connection string redirect)
    /// and from environment variables and command-line arguments, but does not recurse
    /// into the dependencies of those dependencies.
    /// </summary>
    DirectOnly = 1,

    /// <summary>
    /// When set, unresolved values from annotation callbacks will be cached and reused 
    /// on subsequent evaluations of the same annotation, rather than re-evaluating the callback each time.
    /// </summary>
    CacheAnnotationCallbackResults = 2
}
