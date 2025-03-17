// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// FSGroupStrategyOptionsV1Beta1 specifies the strategy options for controlling the FSGroup security context
/// within a Pod Security Policy. This helps define the rules and ranges applicable to the FSGroup value, which
/// specifies a supplemental group applied to the pod's file system.
/// </summary>
[YamlSerializable]
public sealed class FsGroupStrategyOptionsV1Beta1
{
    /// <summary>
    /// Gets or sets the rule that defines the strategy used for managing file system group (FSGroup).
    /// This specifies the policy that determines which FSGroup is applied to volumes in a pod.
    /// </summary>
    [YamlMember(Alias = "rule")]
    public string Rule { get; set; } = null!;

    /// <summary>
    /// Gets the list of allowed ID ranges.
    /// Each element in the list specifies a minimum and maximum value that
    /// define a range of allowed IDs. The ranges are applied to the FSGroup
    /// security settings in Kubernetes for access control purposes.
    /// </summary>
    [YamlMember(Alias = "ranges")]
    public List<IdRangeV1Beta1> Ranges { get; } = [];
}
