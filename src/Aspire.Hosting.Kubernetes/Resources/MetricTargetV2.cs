// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a specification of a target value for a Kubernetes metric in version 2.
/// Defines the target type and value criteria for monitoring and scaling resources.
/// </summary>
[YamlSerializable]
public sealed class MetricTargetV2
{
    /// <summary>
    /// Represents the average value of a metric target. This property specifies
    /// the average value associated with the target metric for monitoring or scaling purposes.
    /// </summary>
    [YamlMember(Alias = "averageValue")]
    public string AverageValue { get; set; } = null!;

    /// <summary>
    /// Represents the type of metric target. This property specifies the kind of metric being targeted,
    /// such as "Value", "AverageValue", or "Utilization", to determine how the metric is interpreted
    /// and calculated for scaling purposes.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Specifies the target value for the metric.
    /// This value is used to determine the desired state of the resource being measured.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target average utilization value for the metric.
    /// This property represents the percentage of resource consumption (or similar utilization metric)
    /// that is used to define optimal scaling for a resource. The value should be provided as an integer
    /// percentage, or null if not specified.
    /// </summary>
    [YamlMember(Alias = "averageUtilization")]
    public int? AverageUtilization { get; set; } = null!;
}
