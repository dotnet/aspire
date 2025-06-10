// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the resource requirements for a container or a pod in a Kubernetes environment.
/// </summary>
/// <remarks>
/// The <see cref="ResourceRequirementsV1"/> class is used to define the resource constraints
/// that a container or pod should adhere to, specified through claims, limits, and requests.
/// </remarks>
[YamlSerializable]
public sealed class ResourceRequirementsV1
{
    /// <summary>
    /// Represents a collection of resource claims associated with the resource requirements.
    /// Each claim defines specific resource requests or constraints.
    /// </summary>
    [YamlMember(Alias = "claims")]
    public List<ResourceClaimV1> Claims { get; } = [];

    /// <summary>
    /// Represents the resource limits for a Kubernetes resource.
    /// Limits specify the maximum amount of resources (e.g., CPU, memory)
    /// that a container can use. The keys represent the resource types,
    /// and the corresponding values specify the quantity limit for each resource.
    /// </summary>
    [YamlMember(Alias = "limits")]
    public Dictionary<string, string> Limits { get; } = [];

    /// <summary>
    /// Gets the resource requests for the container or pod.
    /// </summary>
    /// <remarks>
    /// Represents the minimum amount of each resource type that the container or pod requests.
    /// The resources are identified by their names as keys in the dictionary,
    /// and the corresponding values specify the requested quantity (e.g., CPU or memory).
    /// </remarks>
    [YamlMember(Alias = "requests")]
    public Dictionary<string, string> Requests { get; } = [];
}
