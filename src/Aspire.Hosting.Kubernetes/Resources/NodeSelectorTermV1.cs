// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes node selector term used to define conditions for node selection.
/// </summary>
/// <remarks>
/// NodeSelectorTermV1 is a core component of node affinity rules in Kubernetes scheduling.
/// It enables the specification of multiple match expressions or match fields that collectively
/// define node selection criteria. Each match expression or field is represented using
/// <see cref="NodeSelectorRequirementV1"/>.
/// </remarks>
[YamlSerializable]
public sealed class NodeSelectorTermV1
{
    /// <summary>
    /// Gets the list of match expressions that are used to define the conditions for node selection.
    /// </summary>
    /// <remarks>
    /// MatchExpressions is a collection of node selector requirements, each specifying a key, an operator,
    /// and an optional set of values. This allows defining complex rules for selecting nodes based on
    /// their labels or attributes.
    /// </remarks>
    [YamlMember(Alias = "matchExpressions")]
    public List<NodeSelectorRequirementV1> MatchExpressions { get; } = [];

    /// <summary>
    /// A collection of node selector requirements used to match fields in a Kubernetes node's metadata.
    /// </summary>
    /// <remarks>
    /// MatchFields contains a list of criteria that define field-based conditions
    /// for selecting nodes in Kubernetes. Each condition is represented by an instance
    /// of <see cref="NodeSelectorRequirementV1"/>, which specifies a key, operator,
    /// and optional set of values used for evaluation.
    /// </remarks>
    [YamlMember(Alias = "matchFields")]
    public List<NodeSelectorRequirementV1> MatchFields { get; } = [];
}
