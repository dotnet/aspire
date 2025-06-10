// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the seccomp profile configuration for a Kubernetes resource.
/// Seccomp profiles provide additional security by filtering system calls
/// that interact with the Linux kernel, allowing finer control over the
/// system-level operations accessible to the container or process.
/// </summary>
[YamlSerializable]
public sealed class SeccompProfileV1
{
    /// <summary>
    /// Gets or sets the path to a local file that defines the seccomp profile to be applied.
    /// This property is used to specify a custom seccomp profile from the local file system.
    /// </summary>
    [YamlMember(Alias = "localhostProfile")]
    public string LocalhostProfile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the seccomp profile.
    /// This property specifies the seccomp profile type, which determines the level of security applied to the container process.
    /// Acceptable values for this property may include "RuntimeDefault", "Localhost", or other predefined types depending on the configuration.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;
}
