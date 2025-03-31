// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// ObjectFieldSelectorV1 represents a selector that identifies a specific field of an object.
/// It is used to specify the path to a field in the resource’s object structure
/// and optionally, an API version to interpret the specified field.
/// </summary>
[YamlSerializable]
public sealed class ObjectFieldSelectorV1
{
    /// <summary>
    /// Gets or sets the field path of the object to select.
    /// This property specifies the path to the field within a Kubernetes resource object.
    /// </summary>
    [YamlMember(Alias = "fieldPath")]
    public string FieldPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API version of the referenced Kubernetes resource.
    /// This property specifies the version of the Kubernetes API to which the
    /// referenced resource belongs and is typically used to ensure compatibility
    /// with the desired resource schema.
    /// </summary>
    [YamlMember(Alias = "apiVersion")]
    public string ApiVersion { get; set; } = null!;
}
