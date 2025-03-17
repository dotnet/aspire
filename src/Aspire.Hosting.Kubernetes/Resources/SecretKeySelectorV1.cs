// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// SecretKeySelectorV1 represents a reference to a specific key within a Secret in Kubernetes.
/// It is used to identify and optionally retrieve the key's value from a named Secret resource.
/// </summary>
[YamlSerializable]
public sealed class SecretKeySelectorV1
{
    /// <summary>
    /// Gets or sets the name of the Kubernetes Secret to select the key from.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// Gets or sets an optional boolean value indicating whether the reference to the secret key is mandatory.
    /// If set to false or not specified, the system treats the reference as required.
    /// If set to true, the system allows the reference to be absent or null.
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; } = null!;

    /// <summary>
    /// Specifies the key within the secret to select.
    /// This property is used to reference a specific key contained in a Kubernetes secret.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
