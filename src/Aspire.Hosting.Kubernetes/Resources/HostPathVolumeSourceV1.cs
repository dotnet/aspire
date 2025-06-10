// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a HostPath volume source in Kubernetes. A HostPath volume mounts a directory
/// from the host node's filesystem into a pod. This can be used for scenarios such as sharing
/// data between containers or accessing specific host resources.
/// </summary>
[YamlSerializable]
public sealed class HostPathVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the type for the host path volume.
    /// Specifies the type of the HostPath volume, indicating how the path
    /// should be interpreted by the system. Examples of types include
    /// "DirectoryOrCreate", "FileOrCreate", etc. The value is case-sensitive.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path on the host where the volume source is located.
    /// This is the filesystem path on the host to be mounted into the container.
    /// </summary>
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = null!;
}
