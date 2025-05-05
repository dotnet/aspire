// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration for an image-based volume source in a Kubernetes resource.
/// </summary>
[YamlSerializable]
public sealed class ImageVolumeSourceV1
{
    /// <summary>
    /// Gets or sets the reference to the image resource used in the volume.
    /// This property is used to specify the identifier or link to the image source
    /// associated with the volume.
    /// </summary>
    [YamlMember(Alias = "reference")]
    public string Reference { get; set; } = null!;

    /// <summary>
    /// Specifies the pull policy for the image volume source.
    /// Determines how the container runtime should pull the image, for instance, always pulling a new version,
    /// or using a cached version if available.
    /// </summary>
    [YamlMember(Alias = "pullPolicy")]
    public string PullPolicy { get; set; } = null!;
}
