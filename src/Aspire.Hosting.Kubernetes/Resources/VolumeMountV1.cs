// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a volume mount configuration in a Kubernetes container.
/// </summary>
/// <remarks>
/// This class is used to specify how a volume is mounted into a container,
/// including the path within the container, any sub-paths, mount propagation settings,
/// and read-only configurations.
/// </remarks>
[YamlSerializable]
public sealed class VolumeMountV1
{
    /// <summary>
    /// Gets or sets the name of the volume mount.
    /// This property specifies the identifier for the volume to be mounted,
    /// which is used to reference it within the container.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path within the container at which the volume will be mounted.
    /// </summary>
    [YamlMember(Alias = "mountPath")]
    public string MountPath { get; set; } = null!;

    /// <summary>
    /// Specifies the relative path within the volume from which the container will access data.
    /// This property allows you to mount a specific subdirectory of a volume rather than the root directory.
    /// </summary>
    [YamlMember(Alias = "subPath")]
    public string SubPath { get; set; } = null!;

    /// <summary>
    /// Specifies the mount propagation behavior for the volume mount.
    /// Defines how mounts are propagated from the host to the container and vice versa.
    /// This property is typically used to control sharing of volumes between containers
    /// and impacts the way the volume is mounted in the container environment.
    /// Can be set to specific propagation modes such as "HostToContainer",
    /// "Bidirectional", or left null for default behavior.
    /// </summary>
    [YamlMember(Alias = "mountPropagation")]
    public string? MountPropagation { get; set; }

    /// <summary>
    /// Gets or sets the sub-path expression within the volume mount.
    /// This property allows dynamic sub-paths to be specified using
    /// environment variable substitutions and provides flexibility
    /// for configuring paths at runtime.
    /// </summary>
    [YamlMember(Alias = "subPathExpr")]
    public string? SubPathExpr { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the volume mount is read-only.
    /// </summary>
    /// <remarks>
    /// When set to true, the volume is mounted in a read-only mode where write operations are not allowed.
    /// This property can be null to indicate the absence of an explicit read-only configuration.
    /// </remarks>
    [YamlMember(Alias = "readOnly")]
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the recursive read-only mode configuration for the volume mount.
    /// This property determines if the volume should be mounted in recursive read-only
    /// mode, which enforces read-only access on all subdirectories and files within the volume.
    /// </summary>
    [YamlMember(Alias = "recursiveReadOnly")]
    public string? RecursiveReadOnly { get; set; }
}
