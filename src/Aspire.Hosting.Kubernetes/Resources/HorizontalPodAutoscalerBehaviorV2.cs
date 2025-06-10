// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Defines the scaling behavior for a Horizontal Pod Autoscaler in Kubernetes.
/// </summary>
/// <remarks>
/// The HorizontalPodAutoscalerBehaviorV2 class specifies how the scaling process should behave,
/// including rules for scaling up and scaling down. This can include settings such as
/// stabilization windows and scaling policies to ensure smooth transitions.
/// </remarks>
[YamlSerializable]
public sealed class HorizontalPodAutoscalerBehaviorV2
{
    /// <summary>
    /// Gets or sets the rules that define the behavior for scaling down in a Horizontal Pod Autoscaler (HPA).
    /// This property specifies the conditions and policies associated with decreasing the number of replicas
    /// when scaling down the resources managed by the HPA.
    /// </summary>
    [YamlMember(Alias = "scaleDown")]
    public HpaScalingRulesV2 ScaleDown { get; set; } = new();

    /// <summary>
    /// Specifies the scaling behavior for scaling up operations in the HorizontalPodAutoscaler.
    /// Defines rules, policies, and other configurations governing how the scaling up process should occur.
    /// </summary>
    [YamlMember(Alias = "scaleUp")]
    public HpaScalingRulesV2 ScaleUp { get; set; } = new();
}
