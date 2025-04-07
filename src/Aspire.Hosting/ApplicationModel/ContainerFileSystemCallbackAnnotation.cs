// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a base class for file system entries in a container.
/// </summary>
public abstract class ContainerFileSystemItem
{
    private string? _name;

    /// <summary>
    /// The name of the file or directory. Must be a simple file or folder name and not include any path separators (eg, / or \). To specify parent folders, use one or more <see cref="ContainerDirectory"/> entries.
    /// </summary>
    public string Name
    {
        get => _name!;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

            if (Path.GetDirectoryName(value) != string.Empty)
            {
                throw new ArgumentException("Name must be a simple file or folder name and not include any path separators (eg, / or \\). To specify parent folders, use one or more ContainerDirectory entries.", nameof(value));
            }

            _name = value;
        }
    }

    /// <summary>
    /// The UID of the owner of the file or directory. If set to null, the UID will be inherited from the parent directory or defaults.
    /// </summary>
    public int? Owner { get; set; }

    /// <summary>
    /// The GID of the group of the file or directory. If set to null, the GID will be inherited from the parent directory or defaults.
    /// </summary>
    public int? Group { get; set; }

    /// <summary>
    /// The permissions of the file or directory. If set to 0, the permissions will be inherited from the parent directory or defaults.
    /// </summary>
    public UnixFileMode Mode { get; set; }
}

/// <summary>
/// Represents a file in the container file system.
/// </summary>
public sealed class ContainerFile : ContainerFileSystemItem
{
    /// <summary>
    /// The contents of the file. If null, the file will be created as an empty file.
    /// </summary>
    public string? Contents { get; set; }
}

/// <summary>
/// Represents a directory in the container file system.
/// </summary>
public sealed class ContainerDirectory : ContainerFileSystemItem
{
    /// <summary>
    /// The contents of the directory to create in the container. Will create specified <see cref="ContainerFile"/> and <see cref="ContainerDirectory"/> entries in the directory.
    /// </summary>
    public IEnumerable<ContainerFileSystemItem> Entries { get; set; } = [];
}

/// <summary>
/// Represents a callback annotation that specifies files and folders that should be created or updated in a container.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nw}, DestinationPath = {DestinationPath}")]
public sealed class ContainerFileSystemCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The (absolute) base path to create the new file (and any parent directories) in the container.
    /// This path should already exist in the container.
    /// </summary>
    public required string DestinationPath { get; init; }

    /// <summary>
    /// The UID of the default owner for files/directories to be created or updated in the container. The UID defaults to 0 for root if null.
    /// </summary>
    public int? DefaultOwner { get; init; }

    /// <summary>
    /// The GID of the default group for files/directories to be created or updated in the container. The GID defaults to 0 for root if null.
    /// </summary>
    public int? DefaultGroup { get; init; }

    /// <summary>
    /// The umask to apply to files or folders without an explicit mode permission. If set to null, a default umask value of 0022 (octal) will be used.
    /// The umask takes away permissions from the default permission set (rather than granting them).
    /// </summary>
    /// <remarks>
    /// The umask is a bitmask that determines the default permissions for newly created files and directories. The umask value is subtracted (bitwise masked)
    /// from the maximum possible default permissions to determine the final permissions. For directories, the umask is subtracted from 0777 (rwxrwxrwx) to get
    /// the final permissions and for files it is subtracted from 0666 (rw-rw-rw-). For a umask of 0022, this gives a default folder permission of 0755 (rwxr-xr-x)
    /// and a default file permission of 0644 (rw-r--r--).
    /// </remarks>
    public UnixFileMode? Umask { get; set; }

    /// <summary>
    /// The callback to be executed when the container is created. Should return a tree of <see cref="ContainerFileSystemItem"/> entries to create (or update) in the container.
    /// </summary>
    public required Func<ContainerFileSystemCallbackContext, CancellationToken, Task<IEnumerable<ContainerFileSystemItem>>> Callback { get; init; }
}

/// <summary>
/// Represents the context for a <see cref="ContainerFileSystemCallbackAnnotation"/> callback.
/// </summary>
public sealed class ContainerFileSystemCallbackContext
{
    /// <summary>
    /// A <see cref="IServiceProvider"/> that can be used to resolve services in the callback.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The app model resource the callback is associated with.
    /// </summary>
    public required IResource Model { get; init; }
}