// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a term used to define pod affinity/anti-affinity requirements in Kubernetes.
/// The PodAffinityTermV1 specifies conditions such as label selectors, namespace selectors, and topology keys
/// that determine the placement of pods in relation to other pods in a cluster.
/// </summary>
[YamlSerializable]
public sealed class PodAffinityTermV1
{
    /// <summary>
    /// Gets or sets the label selector used to filter Kubernetes resources based on their labels.
    /// The label selector enables dynamic selection of a specific set of objects
    /// by matching labels or expressions that satisfy the requirements.
    /// </summary>
    /// <remarks>
    /// The label selector may include the following components:
    /// - MatchLabels: A dictionary of key-value pairs where the labels of the resources must match exactly.
    /// - MatchExpressions: A collection of label selector requirements that define more advanced filtering conditions.
    /// </remarks>
    [YamlMember(Alias = "labelSelector")]
    public LabelSelectorV1 LabelSelector { get; set; } = new();

    /// <summary>
    /// Gets or sets the namespace selector used to identify namespaces to which the label selector applies.
    /// This property specifies a label-based filter that restricts the namespaces considered by the associated label selector.
    /// </summary>
    /// <remarks>
    /// The namespace selector is utilized to dynamically select namespaces based on their labels, allowing
    /// for more flexible and dynamic configurations. It operates in conjunction with label selectors
    /// to refine Kubernetes resource targeting.
    /// </remarks>
    [YamlMember(Alias = "namespaceSelector")]
    public LabelSelectorV1 NamespaceSelector { get; set; } = new();

    /// <summary>
    /// A list of label keys that should match for the associated Kubernetes resource.
    /// This property specifies a set of labels that must be present and match among a group
    /// of resources when certain constraints, such as affinity or anti-affinity rules, are applied.
    /// </summary>
    /// <remarks>
    /// This property is intended to work in conjunction with label selectors to filter resources
    /// based on their labels. It helps define stricter conditions for selecting resources during
    /// workload scheduling and resource management in Kubernetes clusters.
    /// </remarks>
    [YamlMember(Alias = "matchLabelKeys")]
    public List<string> MatchLabelKeys { get; } = [];

    /// <summary>
    /// Represents a list of label keys that must NOT be matched by the target resources.
    /// This property is used as part of the label-based selection mechanism to exclude resources
    /// that have specific labels from being selected.
    /// </summary>
    /// <remarks>
    /// MismatchLabelKeys is commonly used in conjunction with MatchLabelKeys and selectors
    /// like LabelSelector or NamespaceSelector to define precise inclusion and exclusion rules
    /// for Kubernetes resource selection. If a resource contains any of the labels specified
    /// in MismatchLabelKeys, it will not match the selector criteria.
    /// </remarks>
    [YamlMember(Alias = "mismatchLabelKeys")]
    public List<string> MismatchLabelKeys { get; } = [];

    /// <summary>
    /// The Namespaces property represents a list of strings that define the specific namespaces
    /// within which the label selector or namespace selector should apply.
    /// This property is particularly useful when specifying criteria for selecting pods
    /// or other Kubernetes resources constrained to particular namespaces.
    /// </summary>
    /// <remarks>
    /// If the Namespaces property is null or empty, the selector will apply across all namespaces.
    /// This property enables fine-grained control over resource selection within the
    /// Kubernetes environment.
    /// </remarks>
    [YamlMember(Alias = "namespaces")]
    public List<string> Namespaces { get; } = [];

    /// <summary>
    /// Specifies the key for the node's topology on which matching should occur.
    /// The value of this property is used to categorize nodes in a cluster based
    /// on specific characteristics, such as region or zone. This is commonly
    /// used in Kubernetes for pod affinity and anti-affinity rules to determine
    /// how pods are scheduled across nodes.
    /// </summary>
    /// <remarks>
    /// The expected value must correspond to a label on the node that denotes
    /// the desired topology classification. It enables controlled distribution
    /// of pods across the nodes based on their topology attributes, ensuring
    /// improved resource utilization and failure isolation.
    /// </remarks>
    [YamlMember(Alias = "topologyKey")]
    public string TopologyKey { get; set; } = null!;
}
