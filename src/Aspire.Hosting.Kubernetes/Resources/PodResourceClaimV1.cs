// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a resource claim specification for a Kubernetes pod.
/// </summary>
/// <remarks>
/// This class is used to define a claim for resources that a Kubernetes pod requires.
/// It specifies the claim's name, the direct reference to an existing resource claim,
/// or the template from which a resource claim can be created when none exists.
/// </remarks>
[YamlSerializable]
public sealed class PodResourceClaimV1
{
    /// <summary>
    /// Gets or sets the name of the resource claim associated with the Pod.
    /// This property represents the alias "name" in the serialized YAML format.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the referenced resource claim associated with the pod.
    /// This is used to identify the resource claim that provides specific resources
    /// to the pod in a Kubernetes environment.
    /// </summary>
    [YamlMember(Alias = "resourceClaimName")]
    public string ResourceClaimName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the resource claim template associated with the pod.
    /// This property specifies the template to be used for creating a resource claim
    /// dynamically, enabling resource allocation for the pod.
    /// </summary>
    [YamlMember(Alias = "resourceClaimTemplateName")]
    public string ResourceClaimTemplateName { get; set; } = null!;
}
