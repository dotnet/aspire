// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the Kubernetes HorizontalPodAutoscaler resource for managing
/// the dynamic scaling of pods based on custom or predefined metrics.
/// </summary>
/// <remarks>
/// The HorizontalPodAutoscaler resource is part of the Kubernetes autoscaling/v1 API version.
/// It automatically adjusts the number of replicas in a replication controller, deployment,
/// or replica set based on observed metrics such as CPU utilization.
/// This class encapsulates the configuration settings, including the scaling target reference
/// and metric thresholds, required to define a HorizontalPodAutoscaler in a Kubernetes cluster.
/// </remarks>
[YamlSerializable]
public sealed class HorizontalPodAutoscaler() : BaseKubernetesResource("autoscaling/v1", "HorizontalPodAutoscaler")
{
    /// <summary>
    /// Gets or sets the specification that defines the desired behavior of the Horizontal Pod Autoscaler.
    /// </summary>
    /// <remarks>
    /// This property holds the configuration details for the autoscaling behavior, including metrics
    /// such as CPU usage, target resource references, and the range of permissible replica counts.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public HorizontalPodAutoscalerSpecV1 Spec { get; set; } = new();
}
