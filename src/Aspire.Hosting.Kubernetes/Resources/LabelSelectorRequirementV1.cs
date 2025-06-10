// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a label selector requirement for Kubernetes resources. This is used to
/// define conditions for filtering resources based on labels. Each requirement consists
/// of a key, an operator, and a set of values that dictate how the label selector behaves.
/// </summary>
[YamlSerializable]
public sealed class LabelSelectorRequirementV1
{
    /// <summary>
    /// Gets or sets the operator to be applied in the context of label selection.
    /// Specifies the relationship between the key and values attributes required to satisfy the condition.
    /// Commonly used operators could include 'In', 'NotIn', 'Exists', or 'DoesNotExist'.
    /// </summary>
    [YamlMember(Alias = "operator")]
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Gets the collection of values that are associated with the label key in a selector requirement.
    /// This property represents a list of string values that must match or be compared against
    /// in accordance with the specified operator. The values are used to define matching
    /// criteria for labels in Kubernetes resources.
    /// </summary>
    [YamlMember(Alias = "values")]
    public List<string> Values { get; } = [];

    /// <summary>
    /// Gets or sets the key that the selector applies to.
    /// This is used to specify the label key that should be matched against in the selector.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
