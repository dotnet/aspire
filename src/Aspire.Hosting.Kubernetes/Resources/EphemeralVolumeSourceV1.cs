// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an ephemeral volume source in Kubernetes.
/// An ephemeral volume is a temporary storage resource that is created and managed along with a pod's lifecycle.
/// This object allows you to specify a PersistentVolumeClaim template to define the properties of the ephemeral volume.
/// </summary>
[YamlSerializable]
public sealed class EphemeralVolumeSourceV1
{
    /// <summary>
    /// VolumeClaimTemplate defines the specification of a PersistentVolumeClaim template
    /// that is created as part of an EphemeralVolumeSource. This property enables the
    /// dynamic provision of storage resources by defining the metadata and spec for the
    /// generated PersistentVolumeClaim objects.
    /// </summary>
    [YamlMember(Alias = "volumeClaimTemplate")]
    public PersistentVolumeClaimTemplateV1 VolumeClaimTemplate { get; set; } = new();
}
