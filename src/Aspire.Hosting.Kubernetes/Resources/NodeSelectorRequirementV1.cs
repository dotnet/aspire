// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a node selector requirement used to constrain a set of nodes in Kubernetes.
/// </summary>
/// <remarks>
/// NodeSelectorRequirementV1 defines a selector requirement as a key-value pair with an operator
/// and an optional set of values. It is typically used in node affinity rules to determine
/// eligibility of nodes for scheduling a workload.
/// </remarks>
[YamlSerializable]
public sealed class NodeSelectorRequirementV1
{
    /// <summary>
    /// Gets or sets the operator used in the node selection requirement.
    /// The operator specifies the key’s relationship to a set of values.
    /// Examples include "In", "NotIn", "Exists", or "DoesNotExist".
    /// </summary>
    [YamlMember(Alias = "operator")]
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Gets a collection of values used in the node selector requirements for specifying constraints.
    /// </summary>
    [YamlMember(Alias = "values")]
    public List<string> Values { get; } = [];

    /// <summary>
    /// Specifies the key that is used to match against node labels or attributes.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
