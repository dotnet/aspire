// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the capabilities configuration for a Kubernetes container.
/// Capabilities allow fine-grained control over kernel-level privileges
/// granted to a specific container in a Pod.
/// </summary>
[YamlSerializable]
public sealed class CapabilitiesV1
{
    /// <summary>
    /// Gets a list of capabilities to add to the container.
    /// </summary>
    /// <remarks>
    /// Adding specific capabilities enhances the container's permissions.
    /// Use this property to specify the capabilities required by the container.
    /// </remarks>
    [YamlMember(Alias = "add")]
    public List<string> Add { get; } = [];

    /// <summary>
    /// Gets a list of capabilities to be dropped from the container's security context.
    /// Dropping capabilities reduces the container's privileges, enhancing security.
    /// </summary>
    [YamlMember(Alias = "drop")]
    public List<string> Drop { get; } = [];
}
