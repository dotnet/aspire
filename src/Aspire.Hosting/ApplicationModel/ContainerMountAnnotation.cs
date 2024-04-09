// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a mount annotation for a container resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Source = {Source}, Target = {Target}")]
public sealed class ContainerMountAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Instantiates a mount annotation that specifies the details for a container mount.
    /// </summary>
    /// <param name="source">The source path if a bind mount or name if a volume. Can be <c>null</c> if the mount is an anonymous volume.</param>
    /// <param name="target">The target path of the mount.</param>
    /// <param name="type">The type of the mount.</param>
    /// <param name="isReadOnly">A value indicating whether the mount is read-only.</param>
    public ContainerMountAnnotation(string? source, string target, ContainerMountType type, bool isReadOnly)
    {
        if (type == ContainerMountType.BindMount)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException(nameof(source), Resources.ContainerMountBindMountsRequireSourceExceptionMessage);
            }

            if (!Path.IsPathRooted(source))
            {
                throw new ArgumentException(Resources.ContainerMountBindMountsRequireRootedPaths, nameof(source));
            }
        }

        if (type == ContainerMountType.Volume && string.IsNullOrEmpty(source) && isReadOnly)
        {
            throw new ArgumentException(Resources.ContainerMountAnonymousVolumesReadOnlyExceptionMessage, nameof(isReadOnly));
        }

        Source = source;
        Target = target;
        Type = type;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets the source of the bind mount or name if a volume. Can be <c>null</c> if the mount is an anonymous volume.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the target of the mount.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the type of the mount.
    /// </summary>
    public ContainerMountType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the volume mount is read-only.
    /// </summary>
    public bool IsReadOnly { get; }
}

/// <summary>
/// Represents the type of a container mount.
/// </summary>
public enum ContainerMountType
{
    /// <summary>
    /// A local directory or file that is mounted into the container.
    /// </summary>
    BindMount,
    /// <summary>
    /// A volume.
    /// </summary>
    Volume
}
