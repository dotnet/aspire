// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the Windows-specific security context options for a container or pod in Kubernetes.
/// </summary>
[YamlSerializable]
public sealed class WindowsSecurityContextOptionsV1
{
    /// <summary>
    /// Gets or sets the contents of the GMSA (Group Managed Service Account) credential specification.
    /// This property provides the serialized credential spec details used for configuring and authorizing
    /// Windows containers to use a GMSA in Kubernetes environments.
    /// </summary>
    [YamlMember(Alias = "gmsaCredentialSpec")]
    public string GmsaCredentialSpec { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the GMSA (Group Managed Service Account) credential specification
    /// to be used for configuring Windows security context for a container.
    /// </summary>
    /// <remarks>
    /// This property identifies the GMSA credential spec configuration by name, which is often used to
    /// configure the access permissions and identity under which a Windows container operates.
    /// The GMSA Credential Spec Name should correspond to a previously configured GMSA resource in the system.
    /// </remarks>
    [YamlMember(Alias = "gmsaCredentialSpecName")]
    public string GmsaCredentialSpecName { get; set; } = null!;

    /// <summary>
    /// Specifies the username of the account to run the process as, within the context of a Windows container.
    /// </summary>
    /// <remarks>
    /// This property is used to set the user identity when executing a process under a specific Windows Security Context.
    /// Ensure the specified username exists and has the appropriate permissions within the container.
    /// </remarks>
    [YamlMember(Alias = "runAsUserName")]
    public string RunAsUserName { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the container should run as a Host Process.
    /// When set to true, it enables the container to run with permissions on the host machine,
    /// effectively allowing operations similar to a process running directly on the host.
    /// This setting is platform-specific and primarily applicable for Windows-based containers.
    /// </summary>
    [YamlMember(Alias = "hostProcess")]
    public bool? HostProcess { get; set; } = null!;
}
