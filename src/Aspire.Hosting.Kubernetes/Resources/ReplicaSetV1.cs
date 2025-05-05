// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes ReplicaSet resource in the `apps/v1` API version.
/// </summary>
/// <remarks>
/// A ReplicaSet ensures that a specified number of pod replicas are running at any given time.
/// It is commonly used to maintain the desired state of a workload by managing the lifecycle of pods.
/// </remarks>
[YamlSerializable]
public sealed class ReplicaSet() : BaseKubernetesResource("apps/v1", "ReplicaSet")
{
    /// <summary>
    /// Gets or sets the specification that describes the desired behavior of the ReplicaSet resource.
    /// </summary>
    /// <remarks>
    /// The specification defines the desired state, including replica count, pod templates, and label selector,
    /// for the Kubernetes ReplicaSet to maintain the specified number of pod replicas.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public ReplicaSetSpecV1 Spec { get; set; } = new();
}
