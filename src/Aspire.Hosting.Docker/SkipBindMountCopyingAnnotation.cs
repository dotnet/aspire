// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Annotation to indicate that a bind mount should not be copied to the output folder
/// during Docker Compose publishing. This is useful for system files, sockets, or other
/// resources that should remain as absolute paths in the compose file.
/// </summary>
/// <param name="sourcePath">The source path of the bind mount that should not be copied.</param>
public sealed class SkipBindMountCopyingAnnotation(string sourcePath) : IResourceAnnotation
{
    /// <summary>
    /// Gets the source path that should not be copied during Docker Compose publishing.
    /// </summary>
    public string SourcePath { get; } = sourcePath;
}