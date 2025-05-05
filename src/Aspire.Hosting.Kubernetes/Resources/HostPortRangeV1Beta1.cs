// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a range of host ports that can be used in Kubernetes deployments.
/// </summary>
[YamlSerializable]
public sealed class HostPortRangeV1Beta1
{
    /// <summary>
    /// Gets or sets the minimum value of the port range.
    /// Represents the smallest port number within this range.
    /// </summary>
    [YamlMember(Alias = "min")]
    public int Min { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum value of the port range.
    /// Represents the upper bound of the host port range in a Kubernetes resource configuration.
    /// Must be greater than or equal to the Min property to define a valid range.
    /// </summary>
    [YamlMember(Alias = "max")]
    public int Max { get; set; } = 1;
}
