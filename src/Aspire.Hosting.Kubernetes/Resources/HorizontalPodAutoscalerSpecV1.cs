// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Specifies the desired behavior of a Horizontal Pod Autoscaler in Kubernetes for the autoscaling/v1 version.
/// </summary>
/// <remarks>
/// The HorizontalPodAutoscalerSpecV1 class defines the configuration for automatically scaling
/// the number of pods based on observed metrics such as CPU utilization. It specifies the
/// target resource to be scaled, the minimum and maximum number of replicas, and the desired
/// CPU utilization threshold for scaling decisions.
/// </remarks>
[YamlSerializable]
public sealed class HorizontalPodAutoscalerSpecV1
{
    /// <summary>
    /// Gets or sets the target CPU utilization percentage for the horizontal pod autoscaler.
    /// </summary>
    /// <remarks>
    /// This property defines the desired CPU utilization percentage threshold for the autoscaler to
    /// maintain. When the average CPU usage across the pods exceeds this value, additional replicas
    /// may be scaled up. Conversely, when the usage drops below this value, replicas may be scaled down.
    /// </remarks>
    [YamlMember(Alias = "targetCPUUtilizationPercentage")]
    public int? TargetCPUUtilizationPercentage { get; set; }

    /// <summary>
    /// Specifies the reference to the target object that the HorizontalPodAutoscaler is scaling.
    /// </summary>
    /// <remarks>
    /// The ScaleTargetRef property is a reference to the Kubernetes resource that the HorizontalPodAutoscaler
    /// will monitor and scale. This reference includes essential details about the target resource, such as
    /// its name and API version, encapsulated in a CrossVersionObjectReferenceV1 object.
    /// </remarks>
    [YamlMember(Alias = "scaleTargetRef")]
    public CrossVersionObjectReferenceV1 ScaleTargetRef { get; set; } = new();

    /// <summary>
    /// Specifies the maximum allowed number of replicas for the target resource managed by the HorizontalPodAutoscaler.
    /// </summary>
    /// <remarks>
    /// The MaxReplicas property defines an upper limit on the number of replicas that can be created for the
    /// specified workload. This ensures that the scaling process does not exceed operational constraints
    /// or resource limits, even if usage metrics surpass thresholds.
    /// </remarks>
    [YamlMember(Alias = "maxReplicas")]
    public int MaxReplicas { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum number of replicas that the HorizontalPodAutoscaler will maintain for the specified resource.
    /// </summary>
    /// <remarks>
    /// This property ensures that the targeted application maintains a guaranteed minimum level of scalability,
    /// preventing the number of replicas from falling below the specified value. Setting this value is optional
    /// and, if not set, the default value may rely on the Kubernetes configuration for the HorizontalPodAutoscaler.
    /// </remarks>
    [YamlMember(Alias = "minReplicas")]
    public int? MinReplicas { get; set; }
}
