// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the affinity configuration for a pod, including node affinity, pod affinity, and pod anti-affinity settings.
/// This class defines rules to influence pod scheduling based on various criteria, such as node labels or inter-pod relationships.
/// </summary>
[YamlSerializable]
public sealed class AffinityV1
{
    /// <summary>
    /// Represents the node affinity property that defines node affinity scheduling rules.
    /// This property allows specifying preferred or required nodes for scheduling pods.
    /// </summary>
    [YamlMember(Alias = "nodeAffinity")]
    public NodeAffinityV1 NodeAffinity { get; set; } = new();

    /// <summary>
    /// Represents inter-pod affinity scheduling rules to influence the placement of pods relative to other pods.
    /// This property defines constraints for scheduling pods to be either co-located or not co-located with
    /// specified pods, based on labels and topology.
    /// </summary>
    [YamlMember(Alias = "podAffinity")]
    public PodAffinityV1 PodAffinity { get; set; } = new();

    /// <summary>
    /// Represents the pod anti-affinity configuration for scheduling in Kubernetes.
    /// Pod anti-affinity allows specifying rules to avoid placing certain pods together
    /// on the same node or in a specific topology domain. This ensures Pods are scheduled
    /// in a manner that prevents tightly coupling their placement.
    /// </summary>
    [YamlMember(Alias = "podAntiAffinity")]
    public PodAntiAffinityV1 PodAntiAffinity { get; set; } = new();
}
