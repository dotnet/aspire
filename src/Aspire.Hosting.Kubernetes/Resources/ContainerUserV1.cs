// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a container user configuration for Kubernetes resources.
/// Provides details about user identity for container environments, specifically for Linux containers.
/// </summary>
[YamlSerializable]
public sealed class ContainerUserV1
{
    /// <summary>
    /// Represents the Linux-specific container user configuration.
    /// Provides the ability to define user identity and group information for Linux containers.
    /// </summary>
    [YamlMember(Alias = "linux")]
    public LinuxContainerUserV1 Linux { get; set; } = new();
}
