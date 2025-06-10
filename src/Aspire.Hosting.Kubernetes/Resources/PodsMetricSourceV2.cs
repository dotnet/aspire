// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a configuration for targeting metrics at the pod level in a Kubernetes environment.
/// Provides specification for a metric associated with pod resources and the target value
/// or criteria for monitoring and scaling purposes.
/// </summary>
/// <remarks>
/// This class is designed for serializing and deserializing Kubernetes pod metric configurations in YAML format.
/// It contains a metric identifier and a target specification to enable monitoring and scaling
/// based on custom or predefined pod metrics.
/// </remarks>
[YamlSerializable]
public sealed class PodsMetricSourceV2
{
    /// <summary>
    /// Represents the metric associated with a Kubernetes PodsMetricSourceV2 resource.
    /// </summary>
    /// <remarks>
    /// This property references a <see cref="MetricIdentifierV2"/> object, which includes
    /// the name of the metric and an optional selector for additional filtering criteria.
    /// It is used to define the metric that the Kubernetes resource should monitor or act upon.
    /// </remarks>
    [YamlMember(Alias = "metric")]
    public MetricIdentifierV2 Metric { get; set; } = new();

    /// <summary>
    /// Defines the target configuration for a Kubernetes metric in version 2.
    /// Specifies the intended target values or utilization for the metric,
    /// used for monitoring and scaling purposes.
    /// </summary>
    [YamlMember(Alias = "target")]
    public MetricTargetV2 Target { get; set; } = new();
}
