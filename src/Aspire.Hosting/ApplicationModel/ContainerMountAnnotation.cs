// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a mount annotation for a container resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Source = {Source}, Target = {Target}")]
public sealed class ContainerMountAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Instantiates a mount annotation that specifies the source and target paths for a mount.
    /// </summary>
    /// <param name="source">The source path of the mount.</param>
    /// <param name="target">The target path of the mount.</param>
    /// <param name="type">The type of the mount.</param>
    /// <param name="isReadOnly">A value indicating whether the mount is read-only.</param>
    public ContainerMountAnnotation(string source, string target, ContainerMountType type, bool isReadOnly)
    {
        Source = source;
        Target = target;
        Type = type;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets or sets the source of the mount.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the target of the mount.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the type of the mount.
    /// </summary>
    public ContainerMountType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the volume mount is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// Represents the type of a container mount.
/// </summary>
public enum ContainerMountType
{
    /// <summary>
    /// A local folder that is mounted into the container.
    /// </summary>
    Bind,
    /// <summary>
    /// A named volume.
    /// </summary>
    Named
}
