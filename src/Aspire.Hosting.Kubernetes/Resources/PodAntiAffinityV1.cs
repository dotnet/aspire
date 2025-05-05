// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the pod anti-affinity rules used in Kubernetes scheduling policies.
/// </summary>
/// <remarks>
/// PodAntiAffinityV1 defines constraints for scheduling pods on nodes based on anti-affinity rules.
/// Anti-affinity is used to avoid placing certain pods close to each other or restrict them from being scheduled
/// on the same nodes within the cluster. This can enhance fault tolerance, resource allocation, or other layout requirements.
/// </remarks>
[YamlSerializable]
public sealed class PodAntiAffinityV1
{
    /// <summary>
    /// A list of weighted pod affinity terms that are considered during scheduling.
    /// This property allows specifying optional pod anti-affinity preferences for scheduling decisions,
    /// while permitting the system to schedule pods in scenarios where the preferences cannot be met.
    /// Each term is associated with a weight that indicates its relative importance.
    /// </summary>
    /// <remarks>
    /// The items in the list define preferences rather than strict requirements. The Kubernetes scheduler
    /// tries to place pods in a manner that satisfies the specified preferences, considering their weights,
    /// but it may also fall back to alternate scheduling strategies if the preferences cannot be accommodated.
    /// This property is primarily used to influence pod placement while maintaining scheduling flexibility.
    /// </remarks>
    [YamlMember(Alias = "preferredDuringSchedulingIgnoredDuringExecution")]
    public List<WeightedPodAffinityTermV1> PreferredDuringSchedulingIgnoredDuringExecution { get; } = [];

    /// <summary>
    /// Represents a list of PodAffinityTermV1 objects used to define required pod anti-affinity constraints
    /// that must be met during pod scheduling but are ignored during execution.
    /// This property ensures that certain pod placement rules are enforced when scheduling occurs;
    /// however, these rules can be relaxed if the constraints are violated at runtime,
    /// such as when a node becomes unavailable or pods are rescheduled for fault recovery.
    /// </summary>
    [YamlMember(Alias = "requiredDuringSchedulingIgnoredDuringExecution")]
    public List<PodAffinityTermV1> RequiredDuringSchedulingIgnoredDuringExecution { get; } = [];
}
