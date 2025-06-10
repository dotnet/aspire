// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to a Kubernetes object with a specified kind and API group.
/// </summary>
/// <remarks>
/// This class is typically used to define a reference to another resource in the Kubernetes API.
/// It includes information about the object's kind, name, namespace, and API group.
/// </remarks>
[YamlSerializable]
public sealed class TypedObjectReferenceV1
{
    /// <summary>
    /// Represents the type of the Kubernetes resource referenced.
    /// This property denotes the kind of the resource (e.g., Pod, Service, Deployment)
    /// being referred to, allowing identification of the specific resource type.
    /// </summary>
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the Kubernetes resource.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the namespace in which the Kubernetes object resides.
    /// </summary>
    [YamlMember(Alias = "namespace")]
    public string Namespace { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API group for the referenced resource.
    /// This typically specifies the group under which the Kubernetes resource is classified,
    /// for example, "apps", "core", or other custom API groups.
    /// </summary>
    [YamlMember(Alias = "apiGroup")]
    public string ApiGroup { get; set; } = null!;
}
