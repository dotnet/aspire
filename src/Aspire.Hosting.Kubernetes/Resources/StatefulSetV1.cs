// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes StatefulSet resource in the "apps/v1" API group.
/// </summary>
/// <remarks>
/// A StatefulSet manages the deployment and scaling of a set of Pods, while ensuring that
/// their order and state are preserved. StatefulSets are typically used for applications
/// that require stable network identities and persistent storage, such as databases or
/// distributed systems.
/// This class provides an abstraction for defining the configuration of a StatefulSet
/// object in YAML, aligning with the Kubernetes specification.
/// </remarks>
/// <example>
/// The StatefulSet class can be used to define the desired state of a set of Pods and
/// associated resources in a Kubernetes cluster. The resource is serialized to YAML
/// for use with Kubernetes manifests.
/// </example>
[YamlSerializable]
public sealed class StatefulSet() : BaseKubernetesResource("apps/v1", "StatefulSet")
{
    /// <summary>
    /// Gets or sets the specification of the Kubernetes StatefulSet resource.
    /// </summary>
    /// <remarks>
    /// Represents the desired state and configuration of a StatefulSet. This property
    /// controls various aspects of the StatefulSet, such as the number of replicas,
    /// pod templates, update strategy, volume claims, and more.
    /// Refer to the <see cref="StatefulSetSpecV1"/> documentation for detailed information
    /// about the available configuration options.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public StatefulSetSpecV1 Spec { get; set; } = new();
}
