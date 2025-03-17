// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a weighted pod affinity term used in Kubernetes scheduling policies.
/// WeightedPodAffinityTermV1 defines a preference for scheduling pods closer to other pods
/// based on affinity/anti-affinity rules.
/// </summary>
/// <remarks>
/// This class is typically utilized in scenarios where a Kubernetes scheduler prioritizes certain
/// affinities while allowing flexibility in placement. The weight determines the level of preference
/// for the associated PodAffinityTermV1.
/// </remarks>
[YamlSerializable]
public sealed class WeightedPodAffinityTermV1
{
    /// <summary>
    /// Represents the pod affinity or anti-affinity requirement used in Kubernetes scheduling.
    /// This property is used to define rules that influence pod placement based on the labels, namespaces,
    /// and topology keys of other pods in a cluster.
    /// </summary>
    /// <remarks>
    /// The PodAffinityTerm allows specifying conditions for controlling pod scheduling by defining criteria such as:
    /// - Label selectors to match specific labels of target pods.
    /// - Namespace selectors to limit the matching pods to specific namespaces.
    /// - A topology key to define the domain that is considered for affinity or anti-affinity rules.
    /// </remarks>
    [YamlMember(Alias = "podAffinityTerm")]
    public PodAffinityTermV1 PodAffinityTerm { get; set; } = new();

    /// <summary>
    /// Gets or sets the weight associated with the pod affinity term.
    /// The weight indicates the importance of the term relative to other terms.
    /// A higher weight implies a stronger preference or priority for satisfying the associated pod affinity term.
    /// </summary>
    [YamlMember(Alias = "weight")]
    public int Weight { get; set; }
}
