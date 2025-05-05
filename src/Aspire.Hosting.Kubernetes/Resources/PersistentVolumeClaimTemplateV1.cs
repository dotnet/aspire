// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a template for creating a PersistentVolumeClaim (PVC) in a Kubernetes environment.
/// </summary>
/// <remarks>
/// This class allows defining metadata and specification for a PersistentVolumeClaim.
/// It is typically used in scenarios where PVCs need to be dynamically created, such as
/// in the context of ephemeral volumes or StatefulSets.
/// </remarks>
[YamlSerializable]
public sealed class PersistentVolumeClaimTemplateV1
{
    /// <summary>
    /// Gets or sets the metadata for the PersistentVolumeClaim template.
    /// The metadata provides additional information about the resource,
    /// including fields such as name, namespace, labels, annotations, and more.
    /// </summary>
    [YamlMember(Alias = "metadata")]
    public ObjectMetaV1 Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the specification for the PersistentVolumeClaim (PVC) resource.
    /// </summary>
    /// <remarks>
    /// The specification defines the desired state of the PVC, including attributes like access modes,
    /// storage class, requested resources, and volume configurations. This property corresponds
    /// to the `spec` field of a Kubernetes PersistentVolumeClaim resource.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public PersistentVolumeClaimSpecV1 Spec { get; set; } = new();
}
