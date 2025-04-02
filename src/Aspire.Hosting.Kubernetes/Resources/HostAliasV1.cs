// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a host alias configuration which maps an IP address to a set of hostnames.
/// This is used for specifying additional host entries in a pod's /etc/hosts file.
/// </summary>
[YamlSerializable]
public sealed class HostAliasV1
{
    /// <summary>
    /// Gets or sets the IP address associated with the HostAlias in the Kubernetes resource definition.
    /// </summary>
    /// <remarks>
    /// This property represents the IP address that will be mapped to the specified hostnames in the resource.
    /// </remarks>
    [YamlMember(Alias = "ip")]
    public string Ip { get; set; } = null!;

    /// <summary>
    /// Represents a collection of hostnames associated with a specific IP address.
    /// This property contains a list of hostnames used for defining aliases.
    /// </summary>
    [YamlMember(Alias = "hostnames")]
    public List<string> Hostnames { get; } = [];
}
