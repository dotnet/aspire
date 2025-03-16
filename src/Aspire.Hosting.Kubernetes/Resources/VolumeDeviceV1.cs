// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a raw block device that is mapped into a Kubernetes container.
/// This class is used to define the name of a volume and the device path in which the volume is mapped
/// on the container.
/// </summary>
[YamlSerializable]
public sealed class VolumeDeviceV1
{
    /// <summary>
    /// Represents the name of the volume device. This is a unique identifier for the volume device
    /// and is used to reference the device within the context of a Kubernetes resource.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path inside the container where the device will be accessible.
    /// This property is required to specify the location in the container's file system
    /// where the device should be mounted or linked.
    /// </summary>
    [YamlMember(Alias = "devicePath")]
    public string DevicePath { get; set; } = null!;
}
