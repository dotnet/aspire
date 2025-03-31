// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents pod affinity rules for Kubernetes pod scheduling.
/// This class defines the pod affinity and anti-affinity constraints
/// that influence the placement of pods during scheduling.
/// </summary>
/// <remarks>
/// Pod affinity is used to dictate that certain pods should be scheduled
/// either in the same topology (e.g., node or zone) as other specified pods
/// or avoid the same topology. It supports both soft preferences and hard requirements
/// for pod placement.
/// </remarks>
[YamlSerializable]
public sealed class PodAffinityV1
{
    /// <summary>
    /// Represents an optional list of weighted pod affinity terms that are considered
    /// during the scheduling phase of a pod's lifecycle, but ignored during the
    /// execution phase.
    /// </summary>
    /// <remarks>
    /// This property is typically used to express soft rules for pod placement, enabling
    /// the Kubernetes scheduler to give higher preference to certain nodes or
    /// configurations without mandating strict enforcement.
    /// </remarks>
    [YamlMember(Alias = "preferredDuringSchedulingIgnoredDuringExecution")]
    public List<WeightedPodAffinityTermV1> PreferredDuringSchedulingIgnoredDuringExecution { get; } = [];

    /// <summary>
    /// Represents a collection of hard affinity rules used during pod scheduling in Kubernetes.
    /// The `RequiredDuringSchedulingIgnoredDuringExecution` property contains a list of
    /// `PodAffinityTermV1` objects, each defining strict constraints that must be met
    /// for pod placement during scheduling. These constraints are mandatory for scheduling
    /// but are not enforced once the pod is running. This allows for ensuring initial
    /// placement conditions while tolerating changes in the cluster environment
    /// after the pod is already scheduled.
    /// </summary>
    [YamlMember(Alias = "requiredDuringSchedulingIgnoredDuringExecution")]
    public List<PodAffinityTermV1> RequiredDuringSchedulingIgnoredDuringExecution { get; } = [];
}
