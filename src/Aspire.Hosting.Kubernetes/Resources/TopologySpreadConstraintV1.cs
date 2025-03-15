// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the topology spread constraints for distributing pods across a Kubernetes cluster.
/// These constraints define policies for balancing pods across different node topologies such as zones or regions.
/// The constraints aim to ensure high availability and resilience by spreading pods evenly based on the specified rules.
/// </summary>
/// <remarks>
/// The following properties define the rules for the constraint:
/// - `WhenUnsatisfiable`: Defines the behavior when the constraint is unsatisfiable. For example, it can be set
/// to "DoNotSchedule" to prevent scheduling pods if the rule cannot be satisfied.
/// - `LabelSelector`: Specifies the selector to identify the set of target pods for applying the constraint.
/// - `MatchLabelKeys`: A list of label key names that must exist on nodes to be considered during the spread calculation.
/// - `MinDomains`: Specifies the minimum number of topology domains that must satisfy the spread constraint.
/// - `MaxSkew`: Defines the maximum skew, which is the difference in the number of pods across the topology domains.
/// An uneven skew would trigger balancing of pods.
/// - `NodeAffinityPolicy`: Determines the policy for using node affinity to influence pod placement.
/// - `NodeTaintsPolicy`: Specifies the policy regarding node taints when scheduling the pods.
/// - `TopologyKey`: Represents the key of a label that defines the topology domains for spreading, such as "zone" or "rack".
/// </remarks>
[YamlSerializable]
public sealed class TopologySpreadConstraintV1
{
    /// <summary>
    /// Specifies the behavior when the constraints defined for topology spreading cannot
    /// be satisfied. This is used to handle scenarios where the node placement does not meet
    /// the desired topology spread constraints.
    /// </summary>
    /// <remarks>
    /// Possible values might include:
    /// - "DoNotSchedule": Prevents the scheduler from placing the pod on a node
    /// that would violate topology constraints.
    /// - "ScheduleAnyway": Allows scheduling on a node despite the constraints not being met.
    /// The exact values and their implications depend on the Kubernetes version and configuration.
    /// </remarks>
    [YamlMember(Alias = "whenUnsatisfiable")]
    public string? WhenUnsatisfiable { get; set; }

    /// <summary>
    /// Gets or sets the label selector used for dynamically filtering Kubernetes resources
    /// based on their labels. This property enables the selection of specific objects
    /// by defining criteria that must match the labels within the resource set.
    /// </summary>
    /// <remarks>
    /// LabelSelector provides mechanisms to define filtering using key-value pairs
    /// (MatchLabels) or complex expressions (MatchExpressions). This helps in tailoring
    /// resource selection based on the requirements of the topology spread constraints.
    /// </remarks>
    [YamlMember(Alias = "labelSelector")]
    public LabelSelectorV1 LabelSelector { get; set; } = new();

    /// <summary>
    /// Defines a list of specific label keys that are required to be matched for topology spreading constraints.
    /// </summary>
    /// <remarks>
    /// This property specifies which label keys should be included for matching when determining
    /// the topology spread constraints of a Kubernetes resource. By specifying these keys, users
    /// can focus on specific labels that must be present for enforcing the desired topological
    /// spread among nodes.
    /// </remarks>
    [YamlMember(Alias = "matchLabelKeys")]
    public List<string> MatchLabelKeys { get; } = [];

    /// <summary>
    /// Specifies the minimum number of topology domains that must have at least one replica
    /// present for resource scheduling. This is used to ensure that replicas are distributed
    /// across a certain number of domains to achieve a balanced topology spread for better availability
    /// and fault tolerance.
    /// </summary>
    /// <remarks>
    /// The value of this property helps enforce the desired distribution of replicas across
    /// different topology domains. For example, the topology domains could represent zones,
    /// regions, or any other logical grouping defined by the cluster.
    /// If this property is not set, the behavior may default to the Kubernetes scheduler's
    /// default logic for topology constraints.
    /// </remarks>
    [YamlMember(Alias = "minDomains")]
    public int? MinDomains { get; set; }

    /// <summary>
    /// Represents the maximum allowed skew for a topology spread constraint in Kubernetes.
    /// Skew is defined as the difference in the number of matching pods between the
    /// target and the other domains defined by the topologyKey.
    /// </summary>
    /// <remarks>
    /// This property is used to control the dispersion of pods across domains, ensuring
    /// that they are evenly balanced based on the specified topologyKey. A lower value
    /// enforces stricter balancing, while a higher value allows more skew in the pod distribution.
    /// </remarks>
    [YamlMember(Alias = "maxSkew")]
    public int? MaxSkew { get; set; }

    /// <summary>
    /// Defines the policy for node affinity within a Kubernetes topology spread constraint.
    /// Determines how nodes are selected to satisfy the specified affinity rules.
    /// </summary>
    /// <remarks>
    /// The policy can control the placement of pods on nodes based on node affinity rules,
    /// ensuring that workloads adhere to specific distribution or colocation requirements.
    /// This property is optional and may be null if no specific node affinity policy is set.
    /// </remarks>
    [YamlMember(Alias = "nodeAffinityPolicy")]
    public string? NodeAffinityPolicy { get; set; }

    /// <summary>
    /// Gets or sets the policy for handling node taints within a topology spread constraint.
    /// </summary>
    /// <remarks>
    /// The NodeTaintsPolicy determines the behavior of the scheduler in deciding how node taints
    /// are considered when applying topology constraints. This property allows fine-grained control
    /// over the interaction between taints and topology spread requirements.
    /// </remarks>
    [YamlMember(Alias = "nodeTaintsPolicy")]
    public string? NodeTaintsPolicy { get; set; }

    /// <summary>
    /// Represents the key used to indicate the topology domain of a Kubernetes resource.
    /// The <c>TopologyKey</c> defines the scope for spreading or balancing resources across nodes
    /// based on the specified topology rule. It is typically matched to a node label, such as
    /// <c>failure-domain.beta.kubernetes.io/zone</c> or <c>kubernetes.io/hostname</c>, to align resources
    /// with the desired domain or affinity policy.
    /// </summary>
    /// <remarks>
    /// This property is used in conjunction with topology constraints to evenly distribute resources
    /// across the specified topology domains or to avoid overloading a single topology domain.
    /// The specified <c>TopologyKey</c> must correspond to a valid label key present on the cluster nodes.
    /// </remarks>
    [YamlMember(Alias = "topologyKey")]
    public string? TopologyKey { get; set; }
}
