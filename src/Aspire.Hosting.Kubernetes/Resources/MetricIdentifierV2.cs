// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a metric identifier in a Kubernetes context, primarily used
/// to identify and specify a metric related to various Kubernetes resources or objects.
/// </summary>
/// <remarks>
/// A metric identifier typically consists of a name that denotes the metric
/// and an optional selector, which is used to specify additional filtering criteria
/// for selecting specific metric instances.
/// This class is designed to be serialized and deserialized in YAML format and is
/// often used in conjunction with other Kubernetes resource configurations.
/// </remarks>
[YamlSerializable]
public sealed class MetricIdentifierV2
{
    /// <summary>
    /// Gets or sets the name of the metric. This property specifies the identifier
    /// for the metric being measured or tracked.
    /// </summary>
    /// <remarks>
    /// The name is used to uniquely identify the metric and can be referenced
    /// in monitoring and metric collection systems.
    /// </remarks>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Represents a label selector used to filter Kubernetes resources. The selector enables
    /// the identification of a subset of objects based on their labels.
    /// </summary>
    /// <remarks>
    /// The selector operates based on two components:
    /// - MatchLabels: A dictionary of label key-value pairs for exact match filtering.
    /// - MatchExpressions: A set of conditions enabling complex filtering logic.
    /// This property allows dynamic selection of targeted resources through labels.
    /// </remarks>
    [YamlMember(Alias = "selector")]
    public LabelSelectorV1 Selector { get; set; } = new();
}
