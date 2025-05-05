// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a mapping of a key from a data source (e.g., ConfigMap or Secret) to a specific file path.
/// </summary>
/// <remarks>
/// The KeyToPathV1 class allows users to define how key-value pairs from a data source
/// should be projected into a volume. Each key is mapped to a file at the specified path,
/// and optional file access modes can be set.
/// </remarks>
[YamlSerializable]
public sealed class KeyToPathV1
{
    /// <summary>
    /// Gets or sets the file mode to be applied to the path.
    /// Represents optional permissions for the file, expressed as an integer.
    /// If not specified, the default mode is utilized.
    /// </summary>
    [YamlMember(Alias = "mode")]
    public int? Mode { get; set; }

    /// <summary>
    /// Represents the file system path where the key should be projected.
    /// This property specifies the target location for the key data within the file system
    /// when handling Kubernetes resources. The value should be a valid file path.
    /// </summary>
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific key to project.
    /// This property represents the key within a data source that will be mapped or projected to a specified path.
    /// </summary>
    [YamlMember(Alias = "key")]
    public string Key { get; set; } = null!;
}
