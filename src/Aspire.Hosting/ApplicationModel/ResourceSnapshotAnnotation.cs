// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that represents the initial snapshot of a resource.
/// </summary>
public class ResourceSnapshotAnnotation(CustomResourceSnapshot initialSnapshot) : IResourceAnnotation
{
    /// <summary>
    /// The initial snapshot of the resource.
    /// </summary>
    public CustomResourceSnapshot InitialSnapshot { get; } = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
}
