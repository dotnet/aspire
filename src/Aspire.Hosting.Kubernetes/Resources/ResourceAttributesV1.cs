// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the attributes of a resource in Kubernetes.
/// This class facilitates the definition of resource attributes for Kubernetes objects,
/// including identifiers like name, namespace, and resource type, as well as selectors
/// for field and label-based filtering.
/// </summary>
[YamlSerializable]
public sealed class ResourceAttributesV1
{
    /// <summary>
    /// Represents the action or operation to be performed on a Kubernetes resource.
    /// The verb typically corresponds to a REST API operation (e.g., "get", "list", "create")
    /// that is associated with the specified resource, namespace, or group.
    /// </summary>
    [YamlMember(Alias = "verb")]
    public string? Verb { get; set; }

    /// <summary>
    /// Gets or sets the name of the Kubernetes resource.
    /// Represents the identifier of a specific resource instance within a particular resource type.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the Kubernetes resource.
    /// The namespace provides a mechanism for isolating groups or sets of resources within a Kubernetes cluster.
    /// If not specified, the default namespace is often used.
    /// </summary>
    [YamlMember(Alias = "namespace")]
    public string? Namespace { get; set; }

    /// <summary>
    /// Represents the name of the resource to which the attributes are associated.
    /// This property specifies the target resource in a Kubernetes environment and is used
    /// for identifying and managing the specific resource within the namespace.
    /// </summary>
    [YamlMember(Alias = "resource")]
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or sets the subresource associated with the Kubernetes resource.
    /// A subresource represents a component or subpart of the primary resource,
    /// commonly used to interact with specific operations or views of the resource.
    /// </summary>
    [YamlMember(Alias = "subresource")]
    public string? Subresource { get; set; }

    /// <summary>
    /// Represents the API version of the Kubernetes resource.
    /// This property specifies the version of the Kubernetes API that the resource belongs to.
    /// </summary>
    [YamlMember(Alias = "version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the API group of the Kubernetes resource.
    /// The group is a way to categorize resources in Kubernetes, typically used along with
    /// the resource and version to identify a specific API resource.
    /// Examples of API groups might include "apps", "core", or custom API groups defined by an extension.
    /// </summary>
    [YamlMember(Alias = "group")]
    public string? Group { get; set; }

    /// <summary>
    /// Represents the field selector attribute used in Kubernetes resources.
    /// This property is used to define field-based filtering criteria for the resource.
    /// The field selector matches resources based on their specific field attributes.
    /// </summary>
    [YamlMember(Alias = "fieldSelector")]
    public FieldSelectorAttributesV1? FieldSelector { get; set; }

    /// <summary>
    /// Represents the label selector attributes used to filter Kubernetes resources
    /// based on their labels. It allows specifying raw selector queries and detailed
    /// requirements for more granular control of resource selection.
    /// </summary>
    [YamlMember(Alias = "labelSelector")]
    public LabelSelectorAttributesV1? LabelSelector { get; set; }
}
