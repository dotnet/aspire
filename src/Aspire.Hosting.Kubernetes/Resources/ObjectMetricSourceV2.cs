// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a source for a metric that is associated with a specific Kubernetes object.
/// </summary>
/// <remarks>
/// This class is part of the Kubernetes metrics API and defines a metric for a target
/// object. It specifies the metric to be observed, the Kubernetes resource (object)
/// being described, and the target value or configuration for that metric.
/// The ObjectMetricSourceV2 is typically used for workloads or resources where metrics
/// are tied to a specific Kubernetes object.
/// </remarks>
[YamlSerializable]
public sealed class ObjectMetricSourceV2
{
    /// <summary>
    /// Represents the metric details associated with an ObjectMetricSourceV2 instance.
    /// </summary>
    /// <remarks>
    /// This property specifies the metric identifier, which includes the metric's name
    /// and an optional selector used for filtering or targeting specific metric instances.
    /// It provides the necessary data to link and describe metrics in relation to a Kubernetes resource.
    /// </remarks>
    [YamlMember(Alias = "metric")]
    public MetricIdentifierV2 Metric { get; set; } = new();

    /// <summary>
    /// Represents the Kubernetes object being described in the metric source.
    /// </summary>
    /// <remarks>
    /// The <c>DescribedObject</c> property is a reference to the specific object in the Kubernetes
    /// cluster to which the metric source applies. This enables metrics to be collected and applied
    /// to custom resources beyond built-in Kubernetes objects.
    /// </remarks>
    [YamlMember(Alias = "describedObject")]
    public CrossVersionObjectReferenceV2 DescribedObject { get; set; } = new();

    /// <summary>
    /// Specifies the target value and criteria for a metric in a Kubernetes resource.
    /// This property defines the desired goal or threshold for the given metric to be used
    /// for monitoring or scaling purposes. It includes attributes such as type, value,
    /// average value, and utilization to determine the metric target.
    /// </summary>
    [YamlMember(Alias = "target")]
    public MetricTargetV2 Target { get; set; } = new();
}
