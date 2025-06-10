// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the attributes used for label selection in Kubernetes resources.
/// </summary>
[YamlSerializable]
public sealed class LabelSelectorAttributesV1
{
    /// <summary>
    /// Gets or sets the raw string representation of a label selector.
    /// This property allows defining label selection criteria in a raw textual format,
    /// which can be used to match resources based on their label key-value pairs.
    /// </summary>
    [YamlMember(Alias = "rawSelector")]
    public string RawSelector { get; set; } = null!;

    /// <summary>
    /// Represents the collection of label selector requirements associated with this object.
    /// Each requirement is a key-value pair that defines a rule for selecting Kubernetes
    /// resources based on their labels.
    /// </summary>
    [YamlMember(Alias = "requirements")]
    public List<LabelSelectorRequirementV1> Requirements { get; } = [];
}
