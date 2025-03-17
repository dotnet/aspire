// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification of a pod template in Kubernetes.
/// </summary>
/// <remarks>
/// A PodTemplateSpec object is primarily used in resource definitions such as ReplicaSets, StatefulSets, and ReplicationControllers
/// to define the template for creating pods. It contains specification details for the pod, including metadata and the pod's desired state.
/// </remarks>
[YamlSerializable]
public sealed class PodTemplateSpecV1
{
    /// <summary>
    /// Gets or sets the metadata associated with the pod template specification.
    /// This includes standard object metadata such as name, namespace, labels,
    /// annotations, and other fields that describe the object.
    /// </summary>
    [YamlMember(Alias = "metadata")]
    public ObjectMetaV1 Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the specification of the pod template.
    /// This property is used to define the desired characteristics and behavior
    /// of the pod, including its containers, volumes, scheduling constraints, and other
    /// configurations. The provided specification follows the structure defined by <see cref="PodSpecV1"/>.
    /// </summary>
    [YamlMember(Alias = "spec")]
    public PodSpecV1 Spec { get; set; } = new();
}
