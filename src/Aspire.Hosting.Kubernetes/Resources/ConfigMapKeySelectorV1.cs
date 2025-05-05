// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// ConfigMapKeySelectorV1 represents a selector for a specific key in a ConfigMap.
/// It is used to select a named key from a ConfigMap, with an optional flag to indicate if the key or ConfigMap is optional.
/// </summary>
[YamlSerializable]
public sealed class ConfigMapKeySelectorV1
{
    /// <summary>
    /// Gets or sets the name of the ConfigMap whose key-value pair is to be selected.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Indicates whether the specific key in the ConfigMap is optional.
    /// If set to true, the absence of the specified key will not raise an error.
    /// If set to false or null, the specified key must exist in the ConfigMap; otherwise, an error may be raised.
    /// </summary>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; }

    /// <summary>
    /// Specifies the key to select from the referenced ConfigMap.
    /// This property identifies a specific entry within the ConfigMap that should be used.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
