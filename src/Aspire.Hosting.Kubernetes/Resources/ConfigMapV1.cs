// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes ConfigMap resource.
/// </summary>
/// <remarks>
/// A ConfigMap is used to store non-confidential data in key-value pairs.
/// Applications running in a Kubernetes cluster can consume this configuration data.
/// Derived from the BaseKubernetesResource class, this class includes properties specific to ConfigMap resources,
/// such as BinaryData, Data, and Immutable, while also inheriting common Kubernetes resource properties like Kind, ApiVersion, and Metadata.
/// </remarks>
[YamlSerializable]
public sealed class ConfigMap() : BaseKubernetesResource("v1", "ConfigMap")
{
    /// <summary>
    /// Represents a collection of binary data entries as key-value pairs within a ConfigMap object.
    /// </summary>
    /// <remarks>
    /// The BinaryData property is used to store binary data in a Kubernetes ConfigMap.
    /// Each entry consists of a unique string key and its corresponding base64-encoded binary value.
    /// Keys must follow the Kubernetes naming conventions.
    /// This property is read-only in the object and needs to be initialized when configuring the ConfigMap.
    /// </remarks>
    [YamlMember(Alias = "binaryData")]
    public Dictionary<string, string> BinaryData { get; } = [];

    /// <summary>
    /// Represents a collection of key-value pairs where both the keys and the values are strings.
    /// </summary>
    /// <remarks>
    /// The Data property is used to store non-binary configuration data in a ConfigMap resource.
    /// Keys must be alphanumeric strings, and values can represent textual configuration details.
    /// This property is a central feature of Kubernetes ConfigMaps, enabling applications
    /// to access configuration data at runtime without requiring changes to application code.
    /// Note that this property is immutable and initialized as an empty dictionary.
    /// </remarks>
    [YamlMember(Alias = "data")]
    public Dictionary<string, string> Data { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the ConfigMap is immutable.
    /// </summary>
    /// <remarks>
    /// When set to true, the ConfigMap becomes immutable, meaning its data and binaryData fields cannot
    /// be modified after creation. This ensures that the resource remains unchanged, which is particularly
    /// useful for use cases where the ConfigMap data must not be altered, such as configuration storage
    /// for applications. When this property is null or false, the ConfigMap can be updated as usual.
    /// </remarks>
    [YamlMember(Alias = "immutable")]
    public bool? Immutable { get; set; }
}
