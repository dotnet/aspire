// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an external metric source in Kubernetes for configuring autoscaling behavior
/// based on metrics from external components or monitoring systems.
/// </summary>
/// <remarks>
/// This class defines the external metric to be monitored and the specific target value
/// associated with it. The `metric` property identifies the external metric by name
/// and optional label selectors. The `target` property provides the desired value,
/// average value, or utilization percentage for the metric, as part of the autoscaling configuration.
/// </remarks>
[YamlSerializable]
public sealed class ExternalMetricSourceV2
{
    /// <summary>
    /// Represents the metric property utilized in the ExternalMetricSourceV2 class.
    /// This property is of type MetricIdentifierV2 and is used to identify and define
    /// a specific metric in the context of Kubernetes resource configurations.
    /// </summary>
    /// <remarks>
    /// The Metric property is typically used to specify the name and any optional selector
    /// for a particular metric, allowing for detailed filtering or identification when
    /// integrating with Kubernetes metrics APIs or external monitoring tools.
    /// </remarks>
    [YamlMember(Alias = "metric")]
    public MetricIdentifierV2 Metric { get; set; } = new();

    /// <summary>
    /// Defines the target configuration for a metric in a Kubernetes-based context.
    /// </summary>
    /// <remarks>
    /// The target specifies the desired state or value for a metric, which is used
    /// to define scaling behavior or monitor system performance. This includes the
    /// type of target, the desired value, and optionally the average value or utilization.
    /// </remarks>
    [YamlMember(Alias = "target")]
    public MetricTargetV2 Target { get; set; } = new();
}
