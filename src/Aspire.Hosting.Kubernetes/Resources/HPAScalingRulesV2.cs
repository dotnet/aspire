// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the scaling rules for Kubernetes Horizontal Pod Autoscaler (HPA) in version 2 API.
/// </summary>
/// <remarks>
/// The HPAScalingRulesV2 class defines the parameters and rules for scaling operations in Kubernetes.
/// It includes configurable scaling policies, a stabilization window, and a strategy for selecting policies.
/// This class enables you to customize the behavior of HPA scaling decisions.
/// </remarks>
[YamlSerializable]
public sealed class HpaScalingRulesV2
{
    /// <summary>
    /// Represents a collection of scaling policies associated with the
    /// Horizontal Pod Autoscaler (HPA) in Kubernetes API v2.
    /// </summary>
    /// <remarks>
    /// The <c>Policies</c> property defines a list of scaling policies that determine how scaling
    /// actions are executed. Each policy specifies the type of scaling operation, the associated value,
    /// and the duration over which the scaling policy is applied.
    /// </remarks>
    [YamlMember(Alias = "policies")]
    public List<HpaScalingPolicyV2> Policies { get; } = [];

    /// <summary>
    /// Gets or sets the stabilization window in seconds for scaling decisions in the Horizontal Pod Autoscaler (HPA).
    /// </summary>
    /// <remarks>
    /// The stabilization window defines the time period during which past metric readings are considered to avoid rapid fluctuations
    /// caused by transient conditions. If set, this property determines how long the system waits before applying a scaling operation.
    /// A null or zero value disables the stabilization window.
    /// </remarks>
    [YamlMember(Alias = "stabilizationWindowSeconds")]
    public int? StabilizationWindowSeconds { get; set; }

    /// <summary>
    /// Specifies the policy selection strategy for scaling operations within the
    /// Horizontal Pod Autoscaler (HPA) configuration.
    /// </summary>
    /// <remarks>
    /// This property determines which policy from the defined list of scaling policies
    /// should be applied when multiple policies are available. Possible values typically
    /// define strategies such as choosing the fastest policy or the one with the largest
    /// or smallest impact.
    /// </remarks>
    [YamlMember(Alias = "selectPolicy")]
    public string SelectPolicy { get; set; } = null!;
}
