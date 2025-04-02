// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents node affinity scheduling configuration for Kubernetes pods.
/// This class defines the rules to determine node selection for a pod during scheduling.
/// Node affinity allows a pod to specify constraints that limit the set of nodes it can be
/// scheduled onto. This includes both hard requirements (requiredDuringSchedulingIgnoredDuringExecution)
/// and soft preferences (preferredDuringSchedulingIgnoredDuringExecution).
/// </summary>
[YamlSerializable]
public sealed class NodeAffinityV1
{
    /// <summary>
    /// A list of preferred scheduling terms that influence the scheduling of a pod while
    /// ignoring the execution of the scheduling preferences. Each term in the list defines
    /// a preference for scheduling pods onto nodes based on specific criteria and associated
    /// weights.
    /// </summary>
    /// <remarks>
    /// The `PreferredDuringSchedulingIgnoredDuringExecution` property defines a set of
    /// preferred scheduling rules that Kubernetes attempts to honor when scheduling a pod.
    /// These preferences are not mandatory, meaning the scheduler may ignore them if nodes
    /// satisfying the preferences are not available. This flexibility ensures that the pod
    /// can still be scheduled even if the preferred nodes are unavailable. Each preference
    /// is represented as an instance of <see cref="PreferredSchedulingTermV1"/>.
    /// </remarks>
    [YamlMember(Alias = "preferredDuringSchedulingIgnoredDuringExecution")]
    public List<PreferredSchedulingTermV1> PreferredDuringSchedulingIgnoredDuringExecution { get; } = [];

    /// <summary>
    /// Specifies node affinity rules that are required during scheduling
    /// but are not enforced during execution.
    /// </summary>
    /// <remarks>
    /// The RequiredDuringSchedulingIgnoredDuringExecution property defines a mandatory set of
    /// rules for scheduling pods onto specific nodes. These rules are considered by the
    /// Kubernetes scheduler at pod scheduling time. However, they are not enforced if the pod
    /// is already running and the node no longer satisfies the selection criteria.
    /// </remarks>
    [YamlMember(Alias = "requiredDuringSchedulingIgnoredDuringExecution")]
    public NodeSelectorV1 RequiredDuringSchedulingIgnoredDuringExecution { get; set; } = new();
}
