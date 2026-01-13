// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that associates a container file/directory with a resource.
/// </summary>
/// <remarks>
/// This annotation is typically used in scenarios where assets, such as images or static files,
/// need to be copied from one container image to another during the build process.
///
/// This annotation is applied to the source resource that produces the files.
/// </remarks>
public sealed class ContainerFilesSourceAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the file system path to the source file or directory inside the container.
    /// </summary>
    public required string SourcePath { get; init; }
}
