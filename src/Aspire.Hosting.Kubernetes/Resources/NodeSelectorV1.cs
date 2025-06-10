// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a node selector in Kubernetes scheduling configuration.
/// The NodeSelectorV1 object contains a list of node selector terms that are used
/// to specify node affinity. Each term specifies a set of match expressions that
/// are evaluated to determine whether a node satisfies the scheduling constraints.
/// </summary>
[YamlSerializable]
public sealed class NodeSelectorV1
{
    /// <summary>
    /// Represents a collection of node selector terms that are used to specify requirements for node selection in Kubernetes.
    /// Each entry in the collection is a <see cref="NodeSelectorTermV1"/> object, which defines a set of conditions to match against nodes.
    /// </summary>
    [YamlMember(Alias = "nodeSelectorTerms")]
    public List<NodeSelectorTermV1> NodeSelectorTerms { get; } = [];
}
