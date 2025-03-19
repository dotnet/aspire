// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the scaling policy configuration for Kubernetes Horizontal Pod Autoscaler (HPA) in version 2 API.
/// </summary>
/// <remarks>
/// This class defines specific policies that determine how scaling operations are performed,
/// including the type of policy, value, and the time period over which the policy is applied.
/// </remarks>
[YamlSerializable]
public sealed class HpaScalingPolicyV2
{
    /// <summary>
    /// Represents the scaling policy type for the Horizontal Pod Autoscaler (HPA) in Kubernetes.
    /// This property determines the strategy to be used for scaling, such as increasing or decreasing replicas.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the scaling value used in the Horizontal Pod Autoscaler (HPA) scaling policy.
    /// Specifies the magnitude of scaling changes to be applied by the policy.
    /// </summary>
    [YamlMember(Alias = "value")]
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the duration, in seconds, for which the scaling policy is applicable.
    /// This value defines the time window during which a HPA scaling action is restricted.
    /// </summary>
    [YamlMember(Alias = "periodSeconds")]
    public int PeriodSeconds { get; set; }
}
