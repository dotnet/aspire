// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the DNS configuration for a Pod in Kubernetes.
/// </summary>
/// <remarks>
/// This class is used to define custom DNS settings, including nameservers, search domains,
/// and DNS configuration options, for a Pod in Kubernetes.
/// </remarks>
[YamlSerializable]
public sealed class PodDnsConfigV1
{
    /// <summary>
    /// Gets the list of IP addresses of DNS servers to be used by the Pod.
    /// </summary>
    /// <remarks>
    /// This property defines the nameservers for the Pod's DNS configuration.
    /// If specified, these nameservers replace the default nameservers provided
    /// by the Kubernetes cluster's DNS configuration.
    /// </remarks>
    [YamlMember(Alias = "nameservers")]
    public List<string> Nameservers { get; } = [];

    /// <summary>
    /// Represents a list of DNS configuration options for a Pod in Kubernetes.
    /// </summary>
    /// <remarks>
    /// Each element in this collection defines a specific DNS configuration option,
    /// encapsulated in the <c>PodDnsConfigOptionV1</c> class. These options allow
    /// fine-tuning of DNS behaviors and settings for the Pod.
    /// </remarks>
    [YamlMember(Alias = "options")]
    public List<PodDnsConfigOptionV1> Options { get; } = [];

    /// <summary>
    /// Gets the list of DNS search domains used for name resolution in the Pod's DNS configuration.
    /// </summary>
    /// <remarks>
    /// The Searches property specifies the DNS search domains that are appended to unqualified
    /// domain names to attempt to resolve them. This is a key part of DNS resolution behavior in
    /// a Kubernetes Pod and helps to define how the Pod resolves DNS queries for host names
    /// that are not fully qualified.
    /// </remarks>
    [YamlMember(Alias = "searches")]
    public List<string> Searches { get; } = [];
}
