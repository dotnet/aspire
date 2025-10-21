// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that associates a static file with a resource.
/// </summary>
/// <remarks>Use this class to specify the source path of a static file that should be linked or embedded as part
/// of a resource. This annotation is typically used in scenarios where static assets, such as images or configuration
/// files, need to be referenced by resource definitions.</remarks>
public sealed class StaticDockerFilesAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the file system path to the source file or directory.
    /// </summary>
    public required string SourcePath { get; init; }

    // async GetBasePathAsync() ? use a callback
}
