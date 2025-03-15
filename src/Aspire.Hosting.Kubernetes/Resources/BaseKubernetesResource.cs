// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Serves as the foundational class for defining Kubernetes resources in the v1 API version.
/// </summary>
/// <remarks>
/// The BaseKubernetesResource class contains shared properties common to all Kubernetes resources,
/// such as Kind, ApiVersion, and Metadata. It acts as an abstract base for deriving specific
/// resource types and facilitates consistent handling of Kubernetes resource definitions.
/// </remarks>
[YamlSerializable]
public abstract class BaseKubernetesResource(string apiVersion, string kind)
{
    /// <summary>
    /// Gets or sets the kind of the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The "kind" property specifies the type of the Kubernetes resource, such as Pod, Deployment, Service, etc.
    /// It serves as a key component in the Kubernetes API schema to identify the type of object being described.
    /// This value is defined by Kubernetes and must match the object's API definition.
    /// </remarks>
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = kind;

    /// <summary>
    /// Gets or sets the API version for the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The API version defines the versioned schema that is used for the resource.
    /// It determines the APIs required to interact with the resource and ensures compatibility
    /// between the resource and Kubernetes components. The value is specified according to the
    /// Kubernetes API versioning scheme (e.g., "v1", "apps/v1").
    /// </remarks>
    [YamlMember(Alias = "apiVersion")]
    public string ApiVersion { get; set; } = apiVersion;

    /// <summary>
    /// Gets or sets the metadata for the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The metadata contains standard information such as the resource’s name, namespace, labels, annotations,
    /// and other Kubernetes-specific properties. It is encapsulated in an <see cref="ObjectMetaV1"/> object,
    /// which provides properties for managing the resource’s unique identifier (UID), name, namespace, generation,
    /// and other relevant details like annotations, labels, and owner references.
    /// </remarks>
    [YamlMember(Alias = "metadata")]
    public ObjectMetaV1 Metadata { get; set; } = new();
}
