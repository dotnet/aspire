// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to a Kubernetes object with a specific type, enabling identification of
/// a local object within the same namespace.
/// </summary>
/// <remarks>
/// This class is primarily used to provide an explicit reference to an object by specifying its kind,
/// name, and optionally its API group. It is commonly utilized in Kubernetes resource definitions
/// that require pointers to other local objects.
/// </remarks>
[YamlSerializable]
public sealed class TypedLocalObjectReferenceV1
{
    /// <summary>
    /// Gets or sets the kind of the referenced resource.
    /// This represents the type of resource being referenced (e.g., "Pod", "ConfigMap").
    /// </summary>
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the referenced resource within the same namespace.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API group of the referent. This property specifies the group of the
    /// referenced Kubernetes resource. An empty string represents the core API group, and a null value
    /// indicates the defaulting behavior is configured.
    /// </summary>
    [YamlMember(Alias = "apiGroup")]
    public string ApiGroup { get; set; } = null!;
}
