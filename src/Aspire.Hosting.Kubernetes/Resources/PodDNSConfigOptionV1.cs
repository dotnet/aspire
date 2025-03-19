// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a DNS configuration option for a Pod in Kubernetes.
/// </summary>
/// <remarks>
/// This class is used as part of the DNS configuration for a Pod in Kubernetes.
/// It allows specifying custom DNS configuration options that can be utilized
/// when the Pod's containers resolve domain names.
/// </remarks>
[YamlSerializable]
public sealed class PodDnsConfigOptionV1
{
    /// <summary>
    /// Gets or sets the name of the DNS configuration option for the pod.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value to be associated with the DNS configuration option.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;
}
