// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes Namespace resource in the v1 API version.
/// </summary>
/// <remarks>
/// The Namespace class defines the structure for the Kubernetes Namespace resource,
/// including metadata and specification details. It inherits from the BaseKubernetesResource
/// class to leverage shared properties such as ApiVersion, Kind, and Metadata. This class
/// enables serialization and deserialization of Namespace resources using YAML.
/// </remarks>
[YamlSerializable]
public sealed class Namespace() : BaseKubernetesResource("v1", "Namespace")
{
    /// <summary>
    /// Gets or sets the specification for the Kubernetes Namespace resource.
    /// </summary>
    /// <remarks>
    /// This property provides access to the namespace specification, which contains
    /// configuration details such as finalizers. Finalizers are used to perform specific
    /// actions or cleanups associated with the namespace before its removal.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public NamespaceSpecV1 Spec { get; set; } = new();
}
