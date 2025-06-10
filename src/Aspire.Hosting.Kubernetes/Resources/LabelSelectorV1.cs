// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a label selector for Kubernetes resources. A label selector is used to filter
/// resources based on their labels, enabling selection of a specific set of objects in a dynamic way.
/// </summary>
/// <remarks>
/// The selector can contain two key components:
/// - MatchLabels: A dictionary of key-value pairs where the resource labels must match exactly.
/// - MatchExpressions: A list of label selector requirements that allow more complex filtering rules.
/// </remarks>
[YamlSerializable]
public sealed class LabelSelectorV1
{
    /// <summary>
    /// Represents a collection of label selector requirements used for matching Kubernetes resources.
    /// Each requirement specifies a key, an operator, and a set of values to define filtering criteria.
    /// This property is used to form more complex selection logic based on multiple conditions.
    /// </summary>
    [YamlMember(Alias = "matchExpressions")]
    public List<LabelSelectorRequirementV1> MatchExpressions { get; set; } = [];

    /// <summary>
    /// A collection of key-value pairs used to specify matching labels for Kubernetes resources.
    /// Labels are utilized as selectors to filter or identify a subset of resources within
    /// a Kubernetes environment.
    /// </summary>
    [YamlMember(Alias = "matchLabels")]
    public Dictionary<string, string> MatchLabels { get; set; } = [];

    /// <summary>
    /// Represents a label selector used to determine a set of resources
    /// in Kubernetes that match the defined criteria.
    /// </summary>
    /// <remarks>
    /// LabelSelectorV1 is commonly used in Kubernetes resource specifications
    /// where filtering objects based on labels is required, such as in ReplicaSets,
    /// Deployments, or custom metrics.
    /// </remarks>
    public LabelSelectorV1()
    {
    }

    /// <summary>
    /// Represents a label selector used to determine a set of resources
    /// in Kubernetes that match the defined criteria.
    /// </summary>
    /// <remarks>
    /// LabelSelectorV1 is commonly used in Kubernetes resource specifications
    /// where filtering objects based on labels is required, such as in ReplicaSets,
    /// Deployments, or custom metrics.
    /// </remarks>
    public LabelSelectorV1(Dictionary<string, string> matchLabels)
    {
        MatchLabels = matchLabels;
    }
}
