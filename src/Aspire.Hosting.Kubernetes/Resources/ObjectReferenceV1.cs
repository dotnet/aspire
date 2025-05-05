// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to an object within a Kubernetes cluster.
/// </summary>
[YamlSerializable]
public sealed class ObjectReferenceV1 : BaseKubernetesObject
{
    /// <summary>
    /// Gets or sets the unique identifier (UID) of the referenced resource.
    /// This is a unique value assigned by Kubernetes to identify the resource instance.
    /// </summary>
    [YamlMember(Alias = "uid")]
    public string Uid { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the Kubernetes resource that this object refers to.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the namespace of the Kubernetes object. This determines the organizational scope
    /// within which the resource resides, typically grouping resources under common logical units
    /// for access control and resource management.
    /// </summary>
    [YamlMember(Alias = "namespace")]
    public string Namespace { get; set; } = null!;

    /// <summary>
    /// Gets or sets the field within the object that the reference points to.
    /// </summary>
    /// <remarks>
    /// The field path is used to refer to a specific field within the referenced object.
    /// This property is particularly useful when working with object selectors or when
    /// referencing sub-fields of an object in Kubernetes resources.
    /// </remarks>
    [YamlMember(Alias = "fieldPath")]
    public string FieldPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific version of the resource. This is used to track changes to the resource
    /// and ensure consistency during updates. The <c>ResourceVersion</c> is typically set by the server
    /// and can be used for optimistic concurrency control when modifying or retrieving resources.
    /// </summary>
    [YamlMember(Alias = "resourceVersion")]
    public string ResourceVersion { get; set; } = null!;
}
