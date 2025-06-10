// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification for a Horizontal Pod Autoscaler (HPA) in version v2 of the Kubernetes Autoscaling API.
/// Provides configuration settings for scaling behavior, target resource, metrics, and replica limits.
/// </summary>
[YamlSerializable]
public sealed class HorizontalPodAutoscalerSpecV2
{
    /// <summary>
    /// Gets or sets the scale target reference for the HorizontalPodAutoscaler.
    /// </summary>
    /// <remarks>
    /// The ScaleTargetRef property specifies the target resource to be scaled by the HorizontalPodAutoscaler.
    /// This includes details such as the name and API version of the target resource.
    /// </remarks>
    [YamlMember(Alias = "scaleTargetRef")]
    public CrossVersionObjectReferenceV2 ScaleTargetRef { get; set; } = new();

    /// <summary>
    /// Specifies the scaling behavior configuration for a Horizontal Pod Autoscaler.
    /// </summary>
    /// <remarks>
    /// This property defines how the Horizontal Pod Autoscaler should behave during scaling operations.
    /// It includes rules and policies for scaling up and scaling down, such as stabilization windows and scaling limits.
    /// </remarks>
    [YamlMember(Alias = "behavior")]
    public HorizontalPodAutoscalerBehaviorV2 Behavior { get; set; } = new();

    /// <summary>
    /// Specifies the maximum number of replicas that a resource, such as a set of pods,
    /// can scale up to using the Horizontal Pod Autoscaler (HPA).
    /// </summary>
    /// <remarks>
    /// The MaxReplicas property defines an upper limit to prevent the workload from growing
    /// beyond a certain size, ensuring that resource utilization remains controlled. This is
    /// a required property and must be greater than or equal to 1.
    /// </remarks>
    [YamlMember(Alias = "maxReplicas")]
    public int MaxReplicas { get; set; } = 1;

    /// <summary>
    /// Gets the list of metrics that determine the desired replica count for the target resource.
    /// </summary>
    /// <remarks>
    /// The metrics define how the scaling behavior of the HorizontalPodAutoscaler is controlled.
    /// Each metric can target various sources, such as external services, resource usage, or object states.
    /// </remarks>
    [YamlMember(Alias = "metrics")]
    public List<MetricSpecV2> Metrics { get; } = [];

    /// <summary>
    /// Specifies the minimum number of replicas that the Horizontal Pod Autoscaler should maintain.
    /// </summary>
    /// <remarks>
    /// This property sets a lower limit on the number of pod replicas that are maintained by the autoscaler.
    /// If not set, it defaults to maintaining at least one instance. A value of 0 can be specified to
    /// allow scaling down to zero replicas when no workload demand exists.
    /// </remarks>
    [YamlMember(Alias = "minReplicas")]
    public int? MinReplicas { get; set; }
}
