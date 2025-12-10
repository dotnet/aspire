// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Options for configuring bind mounts added with <see cref="ContainerResourceBuilderExtensions.WithBindMount{T}(IResourceBuilder{T}, string, string, Action{BindMountOptions})"/>.
/// </summary>
public sealed class BindMountOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the source path should be resolved to an absolute path.
    /// When <c>true</c> (the default), relative paths are resolved relative to the app host project directory.
    /// When <c>false</c>, the source path is passed through as-is without resolution, which is useful for
    /// Docker Compose scenarios where paths should remain relative to the compose file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this to <c>false</c> is useful when publishing to systems like Docker Compose where bind mount
    /// paths should remain relative to the compose file location rather than being resolved to absolute paths.
    /// </para>
    /// </remarks>
    public bool ResolveSourcePath { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bind mount is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}
