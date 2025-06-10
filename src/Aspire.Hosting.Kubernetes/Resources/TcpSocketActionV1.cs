// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a TCP socket action configuration used in Kubernetes resources.
/// This class is typically utilized for health or readiness probes or lifecycle handlers,
/// allowing the definition of connectivity checks to a specified host and port via TCP.
/// </summary>
[YamlSerializable]
public sealed class TcpSocketActionV1
{
    /// <summary>
    /// Specifies the hostname or IP address to be used for the TCP socket action.
    /// </summary>
    [YamlMember(Alias = "host")]
    public string Host { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port number to access for the TCP socket action.
    /// This property specifies the numeric port on which the TCP connection
    /// should be established.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int Port { get; set; }
}
