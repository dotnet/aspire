// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a resource metric source used for Kubernetes scaling operations in version 2.
/// Specifies the name of the resource and its corresponding targeting criteria.
/// </summary>
[YamlSerializable]
public sealed class ResourceMetricSourceV2
{
    /// <summary>
    /// Gets or sets the name of the resource to be monitored for Kubernetes scaling.
    /// This property identifies the specific resource type (e.g., CPU, memory, etc.)
    /// being targeted for metric evaluation.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target details for the resource metric.
    /// This property defines the specifications for the desired target values
    /// or metrics that the resource should aim to maintain, such as average value,
    /// utilization, or specific value thresholds.
    /// </summary>
    [YamlMember(Alias = "target")]
    public MetricTargetV2 Target { get; set; } = new();
}
