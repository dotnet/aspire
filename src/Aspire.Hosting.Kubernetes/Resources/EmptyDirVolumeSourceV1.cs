// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an EmptyDir volume source in Kubernetes, which is an ephemeral volume
/// that is initially empty and then acts as a shared storage space for the containers
/// that are associated with a particular pod. The data in the EmptyDir volume is lost
/// when the pod is removed from a node.
/// </summary>
[YamlSerializable]
public sealed class EmptyDirVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the storage medium to be used for the empty directory volume.
    /// Possible values include "Memory" to use memory backed storage.
    /// If not specified, the default behavior is to use the node's default storage medium.
    /// </summary>
    [YamlMember(Alias = "medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Gets or sets the size limit for the volume.
    /// This specifies the maximum amount of storage space allowed for the volume.
    /// The size must be specified in a valid Kubernetes resource quantity format (e.g., "10Gi", "500Mi").
    /// If no value is specified, the volume will have no size limit and will use available node storage.
    /// </summary>
    [YamlMember(Alias = "sizeLimit")]
    public string? SizeLimit { get; set; }
}
