// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the base class for Kubernetes objects, providing common properties shared across Kubernetes resources.
/// </summary>
[YamlSerializable]
public abstract class BaseKubernetesObject(string? apiVersion = null, string? kind = null)
{
    /// <summary>
    /// Gets or sets the kind of the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The "kind" property specifies the type of the Kubernetes resource, such as Pod, Deployment, Service, etc.
    /// It serves as a key component in the Kubernetes API schema to identify the type of object being described.
    /// This value is defined by Kubernetes and must match the object's API definition.
    /// </remarks>
    [YamlMember(Alias = "kind", Order = -2)]
    public string? Kind { get; set; } = kind;

    /// <summary>
    /// Gets or sets the API version for the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The API version defines the versioned schema that is used for the resource.
    /// It determines the APIs required to interact with the resource and ensures compatibility
    /// between the resource and Kubernetes components. The value is specified according to the
    /// Kubernetes API versioning scheme (e.g., "v1", "apps/v1").
    /// </remarks>
    [YamlMember(Alias = "apiVersion", Order = -3)]
    public string? ApiVersion { get; set; } = apiVersion;
}
