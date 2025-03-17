// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes ConfigMap volume source configuration.
/// </summary>
/// <remarks>
/// A ConfigMapVolumeSourceV1 allows the contents of a Kubernetes ConfigMap to be mounted as a volume.
/// The ConfigMap data can be used directly within the container and optionally mapped to specific paths
/// inside the volume. It also provides the ability to configure default file permissions, specify the
/// target ConfigMap by name, and set optional loading behavior.
/// </remarks>
[YamlSerializable]
public sealed class ConfigMapVolumeSourceV1
{
    /// <summary>
    /// Specifies the default permissions mode for files created within the volume.
    /// This value is represented as an integer and interpreted as an octal value.
    /// If not specified, it defaults to 0644, meaning read/write for owner
    /// and read-only for group and others.
    /// </summary>
    [YamlMember(Alias = "defaultMode")]
    public int? DefaultMode { get; set; }

    /// <summary>
    /// Specifies the name of the ConfigMap. This is a required property that
    /// identifies the ConfigMap to be mounted as a volume.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Specifies whether the ConfigMap or its keys must be defined.
    /// If set to true, the ConfigMap or its keys are optional and
    /// the application will not fail to start if the ConfigMap or keys are missing.
    /// If set to false or omitted, the ConfigMap or its keys are required.
    /// </summary>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; }

    /// <summary>
    /// Gets the collection of key-to-path mappings that specifies how configuration data
    /// from a ConfigMap is mapped to paths within a volume. Each entry in this list maps
    /// a key from the ConfigMap to a specific path in the volume.
    /// </summary>
    [YamlMember(Alias = "items")]
    public List<KeyToPathV1> Items { get; } = [];
}
