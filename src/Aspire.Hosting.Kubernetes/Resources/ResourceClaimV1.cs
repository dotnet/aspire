// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a resource claim for Kubernetes resource management.
/// </summary>
/// <remarks>
/// The <see cref="ResourceClaimV1"/> class is used to define a specific resource claim within a Kubernetes
/// environment, including the name of the resource and its associated request.
/// </remarks>
[YamlSerializable]
public sealed class ResourceClaimV1
{
    /// <summary>
    /// Gets or sets the name of the resource claim.
    /// This identifies the specific resource claim within a Kubernetes resource manifest.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the resource quantity requested by the claim.
    /// </summary>
    [YamlMember(Alias = "request")]
    public string Request { get; set; } = null!;
}
