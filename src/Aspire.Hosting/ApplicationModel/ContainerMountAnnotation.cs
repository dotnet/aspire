// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Resources;

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
        : this(source, target, type, isReadOnly, userId: null, groupId: null, directoryMode: null, fileMode: null)
    {
    }

    /// <summary>
    /// Instantiates a mount annotation that specifies the details for a container mount.
    /// </summary>
    /// <param name="source">The source path if a bind mount or name if a volume. Can be <c>null</c> if the mount is an anonymous volume.</param>
    /// <param name="target">The target path of the mount.</param>
    /// <param name="type">The type of the mount.</param>
    /// <param name="isReadOnly">A value indicating whether the mount is read-only.</param>
    /// <param name="userId">The user ID for ownership. Best-effort; provider may ignore.</param>
    /// <param name="groupId">The group ID for ownership. Best-effort; provider may ignore.</param>
    /// <param name="directoryMode">The default permission mode for directories. Best-effort; provider may ignore.</param>
    /// <param name="fileMode">The default permission mode for files. Best-effort; provider may ignore.</param>
    /// <exception cref="ArgumentException">Thrown when directoryMode or fileMode contains non-permission bits.</exception>
    public ContainerMountAnnotation(string? source, string target, ContainerMountType type, bool isReadOnly,
        int? userId, int? groupId, UnixFileMode? directoryMode, UnixFileMode? fileMode)
    {
        if (type == ContainerMountType.BindMount)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException(nameof(source), MessageStrings.ContainerMountBindMountsRequireSourceExceptionMessage);
            }

            if (!Path.IsPathRooted(source))
            {
                throw new ArgumentException(MessageStrings.ContainerMountBindMountsRequireRootedPaths, nameof(source));
            }
        }

        if (type == ContainerMountType.Volume && string.IsNullOrEmpty(source) && isReadOnly)
        {
            throw new ArgumentException(MessageStrings.ContainerMountAnonymousVolumesReadOnlyExceptionMessage, nameof(isReadOnly));
        }

        // Validate that directoryMode and fileMode only contain permission bits (rwx for user, group, other)
        var allowedPermissionMask = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                  UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                                  UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

        if (directoryMode.HasValue && (directoryMode.Value & ~allowedPermissionMask) != 0)
        {
            throw new ArgumentException("DirectoryMode must contain only permission bits (read, write, execute for user, group, other).", nameof(directoryMode));
        }

        if (fileMode.HasValue && (fileMode.Value & ~allowedPermissionMask) != 0)
        {
            throw new ArgumentException("FileMode must contain only permission bits (read, write, execute for user, group, other).", nameof(fileMode));
        }

        Source = source;
        Target = target;
        Type = type;
        IsReadOnly = isReadOnly;
        UserId = userId;
        GroupId = groupId;
        DirectoryMode = directoryMode;
        FileMode = fileMode;
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

    /// <summary>
    /// Gets the user ID for ownership. This is a best-effort hint and providers may ignore it.
    /// </summary>
    public int? UserId { get; }

    /// <summary>
    /// Gets the group ID for ownership. This is a best-effort hint and providers may ignore it.
    /// </summary>
    public int? GroupId { get; }

    /// <summary>
    /// Gets the default permission mode for directories. This is a best-effort hint and providers may ignore it.
    /// Only permission bits (read, write, execute for user, group, other) are allowed.
    /// </summary>
    public UnixFileMode? DirectoryMode { get; }

    /// <summary>
    /// Gets the default permission mode for files. This is a best-effort hint and providers may ignore it.
    /// Only permission bits (read, write, execute for user, group, other) are allowed.
    /// </summary>
    public UnixFileMode? FileMode { get; }
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
