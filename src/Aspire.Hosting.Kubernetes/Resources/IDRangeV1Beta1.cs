// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a range of IDs with a minimum and maximum value.
/// This can be used to specify a set of allowable IDs for policies or configurations
/// requiring numeric ID ranges (e.g., supplemental groups, run-as-user configurations).
/// </summary>
[YamlSerializable]
public sealed class IdRangeV1Beta1
{
    /// <summary>
    /// Gets or sets the minimum value for the ID range.
    /// </summary>
    [YamlMember(Alias = "min")]
    public long Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value in the ID range. This property represents the upper bound
    /// for the range of IDs within the specified limits.
    /// </summary>
    [YamlMember(Alias = "max")]
    public long Max { get; set; }
}
