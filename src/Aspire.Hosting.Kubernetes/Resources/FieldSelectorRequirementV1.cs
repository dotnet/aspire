// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a requirement used in a field selector in Kubernetes resources.
/// This class is used to specify a filtering condition based on certain attributes
/// or values of a Kubernetes resource.
/// </summary>
[YamlSerializable]
public sealed class FieldSelectorRequirementV1
{
    /// <summary>
    /// Gets or sets the operator used to compare a field key against specified values.
    /// </summary>
    /// <remarks>
    /// The value of this property defines the logical operator to be applied in the field selector
    /// requirement. Commonly used operators include "In", "NotIn", "Exists", and "DoesNotExist".
    /// </remarks>
    [YamlMember(Alias = "operator")]
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Gets the list of values associated with this field selector requirement.
    /// </summary>
    /// <remarks>
    /// This property represents the collection of values that correspond to the
    /// specified key and operator in the field selector requirement. These values
    /// are used to match specific criteria or conditions in Kubernetes resources.
    /// </remarks>
    [YamlMember(Alias = "values")]
    public List<string> Values { get; } = [];

    /// <summary>
    /// Gets or sets the key used in the field selector requirement.
    /// Represents the field key to match within the Kubernetes resource.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
