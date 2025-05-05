// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a local volume source in Kubernetes.
/// This type is used to configure a PersistentVolume that is backed by local storage.
/// </summary>
/// <remarks>
/// Local storage can only be used as a PersistentVolume when the nodeAffinity is also set.
/// The local volume does not support all Kubernetes volume features, such as dynamic provisioning.
/// Local volumes can be beneficial for applications that require high-performance storage
/// and do not require replication.
/// </remarks>
[YamlSerializable]
public sealed class LocalVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the filesystem type to be mounted.
    /// Examples of valid values include "ext4", "xfs", "ntfs", among others.
    /// If not specified, the default filesystem type for the operating system will be used.
    /// </summary>
    [YamlMember(Alias = "fsType")]
    public string FsType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path to the local volume on the host. This value is required and
    /// should specify the file system or directory to be used by the volume.
    /// </summary>
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = null!;
}
