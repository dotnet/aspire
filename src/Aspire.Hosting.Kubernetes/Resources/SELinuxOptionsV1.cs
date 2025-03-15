// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the SELinux options that are applied to a container or pod in Kubernetes.
/// </summary>
/// <remarks>
/// SELinuxOptionsV1 allows you to specify security-enhanced Linux (SELinux) labels for
/// Kubernetes containers or pods. The labels define context-related information, including
/// user, role, type, and level, which are used by the SELinux kernel to enforce security rules.
/// These options are primarily used in environments that enforce SELinux policies for
/// enhanced workload isolation and security.
/// </remarks>
[YamlSerializable]
public sealed class SeLinuxOptionsV1
{
    /// <summary>
    /// Gets or sets the SELinux role for the resource.
    /// This property represents the SELinux role element, which is part of the SELinux
    /// security policy used to define fine-grained access control.
    /// </summary>
    [YamlMember(Alias = "role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SELinux type associated with the SELinuxOptions.
    /// This property specifies the SELinux type of the object. It is
    /// a security attribute used to define the security policy for a resource.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SELinux level for the policy.
    /// This property specifies the SELinux level to apply.
    /// </summary>
    [YamlMember(Alias = "level")]
    public string Level { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SELinux user associated with the security context.
    /// The SELinux user is a required field and specifies the security user policy
    /// applied to the container or process.
    /// </summary>
    [YamlMember(Alias = "user")]
    public string User { get; set; } = null!;
}
