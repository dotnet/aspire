// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the security context configuration for a Kubernetes container or pod.
/// </summary>
/// <remarks>
/// This class encapsulates security settings for containers, such as privilege escalation,
/// user/group IDs, file system configurations, and platform-specific security profiles.
/// </remarks>
[YamlSerializable]
public sealed class SecurityContextV1
{
    /// <summary>
    /// Indicates whether a container should be run with privileged permissions.
    /// This grants the container elevated access to the host system, which can
    /// bypass certain security restrictions. Use with caution as it presents
    /// increased security risks.
    /// </summary>
    [YamlMember(Alias = "privileged")]
    public bool? Privileged { get; set; }

    /// <summary>
    /// Specifies the AppArmor profile configuration for a Kubernetes resource.
    /// AppArmor is a Linux security module that provides mandatory access control
    /// and can restrict programs capabilities with a profile-based policy. This property
    /// defines the details of the AppArmor profile to be applied to the container.
    /// </summary>
    [YamlMember(Alias = "appArmorProfile")]
    public AppArmorProfileV1? AppArmorProfile { get; set; }

    /// <summary>
    /// Specifies the seccomp profile to be applied within the security context of a Kubernetes resource.
    /// Seccomp (Secure Computing Mode) profiles are used to restrict system calls
    /// that applications can make, improving the security posture of containers.
    /// The specified profile determines the system call filtering behavior,
    /// helping enforce least privilege and reduce attack surface.
    /// </summary>
    [YamlMember(Alias = "seccompProfile")]
    public SeccompProfileV1? SeccompProfile { get; set; }

    /// <summary>
    /// Indicates whether the container's filesystem should be configured as read-only.
    /// If true, the root filesystem of the container will be mounted as read-only,
    /// enhancing security by preventing modifications to the filesystem resources.
    /// </summary>
    [YamlMember(Alias = "readOnlyRootFilesystem")]
    public bool? ReadOnlyRootFilesystem { get; set; }

    /// <summary>
    /// Gets or sets a value that determines whether the container is allowed
    /// to gain additional privileges. If set to true, the container is allowed
    /// to elevate its privileges. If false, the container cannot escalate privileges
    /// even if it tries, providing additional security to the container.
    /// </summary>
    [YamlMember(Alias = "allowPrivilegeEscalation")]
    public bool? AllowPrivilegeEscalation { get; set; }

    /// <summary>
    /// Specifies the group ID to run the container's process as.
    /// This property helps define the primary group for file system ownership
    /// and permissions inside the container. If set, the container's process
    /// will run as this group ID. If not set, the group's default ID will be used.
    /// </summary>
    [YamlMember(Alias = "runAsGroup")]
    public long? RunAsGroup { get; set; }

    /// <summary>
    /// Specifies the user ID to run the container process as.
    /// Setting this property provides a security mechanism to ensure
    /// that the container process runs with the specified user privileges
    /// rather than the default root user.
    /// A null value or unset property indicates that the default user ID
    /// defined in the container image or configuration will be used.
    /// </summary>
    [YamlMember(Alias = "runAsUser")]
    public long? RunAsUser { get; set; }

    /// <summary>
    /// Gets or sets the capabilities configuration for a container.
    /// This property is used to define the kernel-level privileges that can
    /// be added or removed for the container, allowing fine-grained control
    /// over security and functionality.
    /// </summary>
    [YamlMember(Alias = "capabilities")]
    public CapabilitiesV1? Capabilities { get; set; }

    /// <summary>
    /// Specifies the SELinux options to be applied to a container.
    /// SELinux options provide fine-grained access control for processes within the container,
    /// ensuring adherence to mandatory access control (MAC) policies.
    /// </summary>
    [YamlMember(Alias = "seLinuxOptions")]
    public SeLinuxOptionsV1? SeLinuxOptions { get; set; }

    /// <summary>
    /// Gets or sets the Windows-specific security context options for the container or pod.
    /// </summary>
    [YamlMember(Alias = "windowsOptions")]
    public WindowsSecurityContextOptionsV1? WindowsOptions { get; set; }

    /// <summary>
    /// Defines the type of /proc mount to be used for a container in a Kubernetes Pod.
    /// This property allows specifying additional visibility or security constraints
    /// on the /proc filesystem, which can help manage access to system-level
    /// operations or information from within the container.
    /// </summary>
    [YamlMember(Alias = "procMount")]
    public string? ProcMount { get; set; }

    /// <summary>
    /// Specifies whether the container must run as a non-root user.
    /// Setting this property to true ensures that the container does not run
    /// with root privileges, enforcing an additional layer of security.
    /// If this property is set to true, the Kubernetes scheduler will
    /// validate that the container does not run as root at runtime.
    /// </summary>
    [YamlMember(Alias = "runAsNonRoot")]
    public bool? RunAsNonRoot { get; set; }
}
