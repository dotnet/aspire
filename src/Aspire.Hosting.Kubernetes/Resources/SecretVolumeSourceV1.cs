// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a volume source based on a Kubernetes Secret.
/// </summary>
/// <remarks>
/// This class allows configuring a Secret as the data source for a Kubernetes volume.
/// The SecretVolumeSourceV1 can be used to specify the secret name, default
/// access permissions, and specific key-to-path mappings for projecting the
/// contents of the secret into the volume. It also allows specifying whether
/// the configuration is optional.
/// </remarks>
[YamlSerializable]
public sealed class SecretVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the default file mode for files created in the volume.
    /// </summary>
    /// <remarks>
    /// The DefaultMode controls the permissions for files written into the volume
    /// when specific file modes are not explicitly defined for individual keys.
    /// The value is typically represented as an integer, expressing file permissions
    /// in bitmask notation (e.g., 0644).
    /// </remarks>
    [YamlMember(Alias = "defaultMode")]
    public int? DefaultMode { get; set; }

    /// <summary>
    /// Gets or sets the name of the Secret to be referenced as a volume.
    /// </summary>
    /// <remarks>
    /// The SecretName property specifies the name of a Kubernetes Secret resource
    /// to mount as a volume. This allows pods to access Secret data, such as sensitive
    /// information, in a filesystem-based layout. When a Secret is mounted as a volume,
    /// the key-value pairs within the Secret are projected as files.
    /// The property should contain the name of an existing Secret in the same namespace
    /// as the Pod. If the referenced Secret does not exist, the Pod will fail to
    /// instantiate unless the Optional property is set to true.
    /// </remarks>
    [YamlMember(Alias = "secretName")]
    public string SecretName { get; set; } = null!;

    /// <summary>
    /// Specifies whether the Secret or its keys must be defined.
    /// </summary>
    /// <remarks>
    /// If set to true, the Secret and its associated data are optional and may not exist.
    /// If set to false or not specified, the Secret is required, and its absence
    /// will result in an error or failure.
    /// </remarks>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; }

    /// <summary>
    /// Gets a list of key-to-path mappings that specify how individual keys within the secret
    /// should be projected into files within the volume.
    /// </summary>
    /// <remarks>
    /// Each entry in the list corresponds to a specific key in the referenced secret and maps it
    /// to a file within the volume. This property allows fine-grained control over which keys are
    /// included in the volume and their corresponding file paths.
    /// </remarks>
    [YamlMember(Alias = "items")]
    public List<KeyToPathV1> Items { get; } = [];
}
