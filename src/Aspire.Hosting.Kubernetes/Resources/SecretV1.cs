// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes Secret resource in the v1 API version.
/// </summary>
/// <remarks>
/// The <c>Secret</c> class is used to store sensitive information, such as passwords, OAuth tokens, and other secrets.
/// Secrets are encoded as key-value pairs and can be mounted into containers or referenced by other Kubernetes resources.
/// By default, the data is Base64-encoded, and alternative string-based representations can also be used.
/// </remarks>
[YamlSerializable]
public sealed class Secret() : BaseKubernetesResource("v1", "Secret")
{
    /// <summary>
    /// Represents a collection of base64-encoded data entries within a Kubernetes Secret resource.
    /// </summary>
    /// <remarks>
    /// The Data property serves as a storage mechanism for sensitive data, such as credentials
    /// or configurations, within a Kubernetes Secret object. The property uses a dictionary
    /// structure, where the keys correspond to the names of the data entries, and the values
    /// are the base64-encoded strings representing the actual data content.
    /// It is important to note that Data values must be encoded in base64 format before assignment.
    /// When managing secrets, Kubernetes ensures that these values are securely handled and
    /// accessible only within the designated scope provided by the resource.
    /// </remarks>
    [YamlMember(Alias = "data")]
    public Dictionary<string, string> Data { get; } = [];

    /// <summary>
    /// Represents a dictionary of non-base64 encoded strings that will be used to populate a Kubernetes Secret object.
    /// </summary>
    /// <remarks>
    /// The StringData property allows users to provide secret data as plain text values instead of base64-encoded strings.
    /// When the Kubernetes API processes the secret manifest, these string values will be encoded into base64 and stored
    /// in the `Data` field of the Secret. This is a convenience feature to simplify secret creation for developers.
    /// </remarks>
    [YamlMember(Alias = "stringData")]
    public Dictionary<string, string> StringData { get; } = [];

    /// <summary>
    /// Indicates whether the Secret is immutable.
    /// </summary>
    /// <remarks>
    /// When set to true, the Secret object becomes immutable, meaning that its contents cannot be altered after creation.
    /// This ensures the integrity of the data stored in the Secret and prevents unintended modifications.
    /// If null or set to false, the Secret can be updated.
    /// </remarks>
    [YamlMember(Alias = "immutable")]
    public bool? Immutable { get; set; } = null!;

    /// <summary>
    /// Represents the type of the Kubernetes Secret resource.
    /// </summary>
    /// <remarks>
    /// The Type property specifies the type of data stored within the Secret. It determines the intended use case
    /// or interpretation of the stored secret data. The default value is "Opaque," which is used for arbitrary
    /// user-defined data.
    /// </remarks>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "Opaque";
}
