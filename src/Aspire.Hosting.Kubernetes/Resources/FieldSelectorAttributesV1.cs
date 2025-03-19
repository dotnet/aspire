// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the attributes used for field selection in Kubernetes resources.
/// This class is designed to define constraints or conditions for selecting specific fields
/// within Kubernetes resources, based on field keys and their associated values.
/// </summary>
[YamlSerializable]
public sealed class FieldSelectorAttributesV1
{
    /// <summary>
    /// Represents the raw string form of a field selector in Kubernetes resources.
    /// This property allows specifying a field selector directly as a raw string
    /// without using structured fields.
    /// </summary>
    [YamlMember(Alias = "rawSelector")]
    public string RawSelector { get; set; } = null!;

    /// <summary>
    /// Gets the collection of field selector requirements used to filter Kubernetes resources.
    /// Each requirement specifies a condition for selecting resources
    /// based on specific attributes or properties.
    /// </summary>
    [YamlMember(Alias = "requirements")]
    public List<FieldSelectorRequirementV1> Requirements { get; } = [];
}
