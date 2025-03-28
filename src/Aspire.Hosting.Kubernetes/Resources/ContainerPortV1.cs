// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration for a container port in Kubernetes.
/// </summary>
/// <remarks>
/// A container port specifies the port mappings and settings used by containers
/// to expose services to the host machine or other containers. This includes
/// the container's port, optional host port, IP bindings, protocol, and an optional
/// name for the port.
/// </remarks>
/// <properties>
/// <item>
/// <term>HostIP</term>
/// <description>The IP address on the host to which the port is bound.</description>
/// </item>
/// <item>
/// <term>Name</term>
/// <description>The name of the container port. This is used as a reference in configurations.</description>
/// </item>
/// <item>
/// <term>Protocol</term>
/// <description>The protocol used for the port (e.g., TCP, UDP).</description>
/// </item>
/// <item>
/// <term>ContainerPort</term>
/// <description>The port number exposed on the container.</description>
/// </item>
/// <item>
/// <term>HostPort</term>
/// <description>The port number exposed on the host machine. This field is optional.</description>
/// </item>
/// </properties>
[YamlSerializable]
public sealed class ContainerPortV1
{
    /// <summary>
    /// Represents the host IP address to which the port is bound.
    /// </summary>
    [YamlMember(Alias = "hostIP")]
    public string HostIp { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the container port.
    /// This property serves as an identifier for the port and can be used
    /// for mapping or referencing purposes in the Kubernetes configuration.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the protocol used by the port. Common protocols include "TCP" and "UDP".
    /// </summary>
    [YamlMember(Alias = "protocol")]
    public string Protocol { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port number on the container where the application is running.
    /// </summary>
    /// <remarks>
    /// This property specifies the port inside the container to which the external traffic
    /// or internal service communication is directed. It is required to define this property
    /// for the proper routing of network traffic within a containerized application.
    /// </remarks>
    [YamlMember(Alias = "containerPort")]
    public Int32OrStringV1? ContainerPort { get; set; }

    /// <summary>
    /// Gets or sets the port number on the host machine that is mapped to the container's port.
    /// This enables external access to the container's service.
    /// </summary>
    [YamlMember(Alias = "hostPort")]
    public Int32OrStringV1? HostPort { get; set; }
}
