// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the AppArmor profile configuration for a Kubernetes resource.
/// </summary>
[YamlSerializable]
public sealed class AppArmorProfileV1
{
    /// <summary>
    /// Gets or sets the name of the local host profile for AppArmor.
    /// This property specifies the custom profile to be used for defining security policies
    /// for the application on the local host.
    /// </summary>
    [YamlMember(Alias = "localhostProfile")]
    public string LocalhostProfile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the AppArmor profile. This property indicates
    /// the kind of AppArmor configuration to be applied, and its value determines
    /// the specific behavior or rules enforced by AppArmor for a workload or container.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;
}
