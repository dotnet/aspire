// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration for mounting a PersistentVolumeClaim as a volume source.
/// </summary>
[YamlSerializable]
public sealed class PersistentVolumeClaimVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the name of the Persistent Volume Claim (PVC).
    /// This property specifies the name of the PVC that will be used as a volume source.
    /// </summary>
    [YamlMember(Alias = "claimName")]
    public string ClaimName { get; set; } = null!;

    /// <summary>
    /// Specifies whether the volume is mounted as read-only.
    /// If set to true, write operations on the volume are disabled.
    /// If not set or set to false, the volume can be mounted with write permissions.
    /// </summary>
    [YamlMember(Alias = "readOnly")]
    public bool? ReadOnly { get; set; }
}
