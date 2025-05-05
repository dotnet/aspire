// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// ConfigMapEnvSourceV1 represents a reference to a ConfigMap that provides key-value pair
/// data for environment variables.
/// The specified ConfigMap is used to populate the environment variables in a container.
/// </summary>
[YamlSerializable]
public sealed class ConfigMapEnvSourceV1
{
    /// <summary>
    /// Gets or sets the name of the ConfigMap resource being referenced.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the ConfigMap is optional.
    /// If true, the ConfigMap reference is considered optional, and the application
    /// will continue running even if the ConfigMap is not found. If false or not set,
    /// the absence of the ConfigMap will result in an error.
    /// </summary>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; }
}
