// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the security context settings for a Kubernetes Pod.
/// </summary>
/// <remarks>
/// The <see cref="PodSecurityContextV1"/> class provides configuration options for controlling
/// security-related attributes of a Kubernetes Pod. These settings include user and group ID
/// management, AppArmor profiles, seccomp profiles, SELinux options, sysctl settings, Windows-specific
/// security options, and more.
/// </remarks>
[YamlSerializable]
public sealed class PodSecurityContextV1
{
    /// <summary>
    /// Represents the AppArmor profile configuration associated with the pod security context.
    /// This property defines the AppArmor settings that are applied to the containers
    /// in the pod, such as profile type and/or specific profiles applied for runtime security.
    /// </summary>
    /// <remarks>
    /// AppArmor allows defining mandatory access control policies for applications.
    /// When set, this property specifies the AppArmor profile details, enabling fine-grained
    /// security controls on containerized workloads.
    /// </remarks>
    [YamlMember(Alias = "appArmorProfile")]
    public AppArmorProfileV1? AppArmorProfile { get; set; }

    /// <summary>
    /// Specifies the Seccomp (Secure Computing Mode) profile configuration for a pod or container
    /// in Kubernetes to restrict system calls made by workloads to enhance security.
    /// </summary>
    /// <remarks>
    /// The SeccompProfile helps define how system calls are filtered and managed for the workload.
    /// It can specify which system calls are allowed or denied by the operating system
    /// through predefined or local profiles.
    /// </remarks>
    [YamlMember(Alias = "seccompProfile")]
    public SeccompProfileV1? SeccompProfile { get; set; }

    /// <summary>
    /// Gets or sets the file system group ID (fsGroup) to be applied to all
    /// volumes mounted in the pod if the volume's security policy supports it.
    /// The ownership of the volumes and permissions may be modified
    /// based on this ID to ensure the designated fsGroup has the required access.
    /// </summary>
    [YamlMember(Alias = "fsGroup")]
    public long? FsGroup { get; set; }

    /// <summary>
    /// Specifies the primary group ID for processes that will run in the container or pod.
    /// This property allows you to control the group ownership for files and processes
    /// within the pod, ensuring consistent group-level permissions during runtime.
    /// </summary>
    [YamlMember(Alias = "runAsGroup")]
    public long? RunAsGroup { get; set; }

    /// <summary>
    /// Specifies the user ID to run the container or pod processes as.
    /// If set, this overrides the user ID specified in the container image or runtime default.
    /// </summary>
    [YamlMember(Alias = "runAsUser")]
    public long? RunAsUser { get; set; }

    /// <summary>
    /// Defines the SELinux options that control the security labeling applied to
    /// the pod or container. SELinuxOptions are part of the SELinux security
    /// mechanism in Linux, allowing fine-grained access control and isolation.
    /// </summary>
    [YamlMember(Alias = "seLinuxOptions")]
    public SeLinuxOptionsV1? SeLinuxOptions { get; set; }

    /// <summary>
    /// Gets the list of supplementary group IDs that are applied to the container's
    /// process. Supplemental groups provide additional Unix group IDs that the
    /// container's main process should run as, in addition to the primary group.
    /// This property is typically used to grant access permissions to resources
    /// shared by multiple Unix groups.
    /// </summary>
    [YamlMember(Alias = "supplementalGroups")]
    public List<long> SupplementalGroups { get; } = [];

    /// <summary>
    /// Represents a collection of kernel parameters (sysctls) for a pod in Kubernetes.
    /// Sysctls are used to configure the kernel parameters at runtime, affecting
    /// the behavior of the operating system for the container.
    /// </summary>
    [YamlMember(Alias = "sysctls")]
    public List<SysctlV1> Sysctls { get; } = [];

    /// <summary>
    /// Represents Windows-specific security context options for a Kubernetes pod or container.
    /// Provides customization settings for Windows-based environments.
    /// </summary>
    [YamlMember(Alias = "windowsOptions")]
    public WindowsSecurityContextOptionsV1? WindowsOptions { get; set; }

    /// <summary>
    /// Specifies whether the container should run as a non-root user.
    /// If set to true, it enforces that the container does not run as a root user.
    /// A value of null indicates no explicit preference.
    /// </summary>
    [YamlMember(Alias = "runAsNonRoot")]
    public bool? RunAsNonRoot { get; set; }

    /// <summary>
    /// Gets or sets the policy that determines when to change the group ownership
    /// of files within the volume mounted in a pod. It specifies how and when
    /// Kubernetes manages the ownership change for the specified `fsGroup`.
    /// Possible values are typically "Always" or "OnRootMismatch".
    /// </summary>
    [YamlMember(Alias = "fsGroupChangePolicy")]
    public string FsGroupChangePolicy { get; set; } = null!;

    /// <summary>
    /// Specifies the policy for handling supplemental groups in the security context of a Kubernetes pod.
    /// This property determines how the system assigns or enforces supplemental groups for the containers
    /// within the pod. It allows for the control of additional group memberships that the
    /// container processes can utilize beyond the primary group.
    /// </summary>
    [YamlMember(Alias = "supplementalGroupsPolicy")]
    public string SupplementalGroupsPolicy { get; set; } = null!;
}
