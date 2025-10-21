// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies a static file resource to be used as the destination for a resource
/// operation.
/// </summary>
/// <remarks>Use this annotation to associate a resource with its required static files, enabling consumers to
/// access or reference those files as part of resource processing. This type is typically used in scenarios where
/// static file dependencies must be declared or tracked alongside resource metadata.</remarks>
public sealed class StaticDockerFileDestinationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the resource that provides access to static files required by this instance.
    /// </summary>
    public required IResource Source { get; init; }

    /// <summary>
    /// Gets or sets the file system path where the static files will be saved.
    /// </summary>
    public required string DestinationPath { get; init; }
}
