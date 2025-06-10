// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a metric source that is defined by a resource usage of a specific container.
/// This is used for scaling purposes in a Kubernetes environment, relying on container-level metrics.
/// </summary>
[YamlSerializable]
public sealed class ContainerResourceMetricSourceV2
{
    /// <summary>
    /// Gets or sets the name of the resource metric.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the container associated with the metric.
    /// It specifies the container within a pod to which the resource metric applies.
    /// </summary>
    [YamlMember(Alias = "container")]
    public string Container { get; set; } = null!;

    /// <summary>
    /// Specifies the target for the metric being monitored in a container.
    /// Represents an instance of <see cref="MetricTargetV2"/> which defines
    /// the thresholds and values for the metric.
    /// </summary>
    [YamlMember(Alias = "target")]
    public MetricTargetV2 Target { get; set; } = new();
}
