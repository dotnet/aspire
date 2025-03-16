// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a toleration configuration for Kubernetes pods.
/// Used to tolerate taints that would otherwise prevent a pod from being scheduled
/// onto a node. Defines the behavior of tolerating taints based on key-value pairs,
/// operators, effects, and optional toleration durations.
/// </summary>
[YamlSerializable]
public sealed class TolerationV1
{
    /// <summary>
    /// Gets or sets the value associated with the toleration.
    /// Typically defines the specific matching value for a taint's key, representing the condition the toleration satisfies.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Specifies the operator that is applied to the key in a Kubernetes toleration.
    /// This property determines the way the key and value interact in the toleration specification.
    /// </summary>
    [YamlMember(Alias = "operator")]
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Specifies the duration (in seconds) for which a pod can tolerate a taint on a node.
    /// If this value is not set, the pod tolerates the taint indefinitely.
    /// </summary>
    [YamlMember(Alias = "tolerationSeconds")]
    public long? TolerationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the taint effect to tolerate.
    /// Represents the taint effect that the toleration is associated with.
    /// Common values include "NoSchedule", "PreferNoSchedule", or "NoExecute".
    /// </summary>
    [YamlMember(Alias = "effect")]
    public string Effect { get; set; } = null!;

    /// <summary>
    /// Gets or sets the key used to identify a specific taint in Kubernetes scheduling rules.
    /// This property represents the label key that the toleration matches.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
