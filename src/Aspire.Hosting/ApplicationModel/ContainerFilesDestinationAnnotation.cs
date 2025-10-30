// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies a source resource and destination path for copying container files.
/// </summary>
/// <remarks>
/// This annotation is typically used in scenarios where assets, such as images or static files,
/// need to be copied from one container image to another during the build process.
///
/// This annotation is applied to the destination resource where the source container's files will be copied to.
/// </remarks>
public sealed class ContainerFilesDestinationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the resource that provides access to the container files to be copied.
    /// </summary>
    public required IResource Source { get; init; }

    /// <summary>
    /// Gets or sets the file system path where the container files will be copied into the destination.
    /// </summary>
    public required string DestinationPath { get; init; }
}
