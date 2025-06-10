// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes resource for Horizontal Pod Autoscaling (HPA) under the autoscaling/v2 API version.
/// </summary>
/// <remarks>
/// The HorizontalPodAutoscalerV2 class is responsible for defining and managing the behavior of the HPA,
/// which automatically adjusts the number of pods in a deployment, replica set, or stateful set based on metrics and thresholds.
/// It extends BaseKubernetesResource to include additional properties specific to the HPA specification as defined in Kubernetes.
/// </remarks>
[YamlSerializable]
public sealed class HorizontalPodAutoscalerV2() : BaseKubernetesResource("autoscaling/v2", "HorizontalPodAutoscaler")
{
    /// <summary>
    /// Gets or sets the specification of the Horizontal Pod Autoscaler (HPA) in version v2 of the Kubernetes Autoscaling API.
    /// </summary>
    /// <remarks>
    /// This property defines the scaling behavior and configuration for a Kubernetes resource targeted by the HPA.
    /// It includes details such as scaling policies, target resource reference, metrics to monitor,
    /// and replica count constraints (minimum and maximum replicas).
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public HorizontalPodAutoscalerSpecV2 Spec { get; set; } = new();
}
