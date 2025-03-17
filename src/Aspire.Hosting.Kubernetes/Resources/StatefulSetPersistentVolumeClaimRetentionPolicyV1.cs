// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the retention policy for PersistentVolumeClaims (PVCs) associated with a StatefulSet in Kubernetes.
/// </summary>
/// <remarks>
/// The StatefulSetPersistentVolumeClaimRetentionPolicyV1 class specifies the behavior of PersistentVolumeClaims
/// when a StatefulSet is either deleted or scaled down. It defines retention behaviors through the properties
/// WhenDeleted and WhenScaled.
/// </remarks>
[YamlSerializable]
public sealed class StatefulSetPersistentVolumeClaimRetentionPolicyV1
{
    /// <summary>
    /// Gets or sets the policy that determines the retention behavior of
    /// PersistentVolumeClaims (PVCs) when a StatefulSet is deleted.
    /// This property defines how PVCs associated with the StatefulSet
    /// should be handled upon deletion of the StatefulSet resource.
    /// </summary>
    [YamlMember(Alias = "whenDeleted")]
    public string? WhenDeleted { get; set; }

    /// <summary>
    /// Gets or sets the policy for handling Persistent Volume Claims (PVCs)
    /// when a StatefulSet is scaled. This property determines how PVCs are
    /// retained or deleted based on scaling operations for the StatefulSet.
    /// </summary>
    [YamlMember(Alias = "whenScaled")]
    public string? WhenScaled { get; set; }
}
