// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a preferred scheduling term used in Kubernetes node affinity configuration.
/// Defines a preference for scheduling pods onto nodes based on specific criteria with an associated weight.
/// </summary>
/// <remarks>
/// This class is part of the Kubernetes scheduling configuration, specifically for preferred scheduling constraints.
/// Each instance of this class includes a preference and a weight:
/// - The `Preference` property indicates the criteria for selecting nodes using a `NodeSelectorTermV1`.
/// - The `Weight` property specifies the relative importance of this term when multiple terms are evaluated.
/// The scheduler attempts to schedule pods on nodes that satisfy the preference criteria with the highest weight values,
/// but these preferences are not strict and do not prevent scheduling on nodes that do not fulfill them.
/// </remarks>
[YamlSerializable]
public sealed class PreferredSchedulingTermV1
{
    /// <summary>
    /// Represents the preference for Kubernetes node scheduling policies.
    /// </summary>
    /// <remarks>
    /// This property specifies a <see cref="NodeSelectorTermV1"/> object, which defines
    /// node selection criteria for scheduling Kubernetes pods. The preference is used
    /// in conjunction with a weight to influence scheduling decisions.
    /// </remarks>
    [YamlMember(Alias = "preference")]
    public NodeSelectorTermV1 Preference { get; set; } = new();

    /// <summary>
    /// Represents the weight assigned to a preferred scheduling condition.
    /// </summary>
    /// <remarks>
    /// Weight determines the priority of a specific preference in node affinity rules when scheduling
    /// workloads in Kubernetes. A higher weight increases the likelihood that this preference will influence
    /// the node selection process.
    /// </remarks>
    [YamlMember(Alias = "weight")]
    public int Weight { get; set; }
}
