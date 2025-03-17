// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a handler for Kubernetes lifecycle events.
/// </summary>
/// <remarks>
/// A LifecycleHandlerV1 defines actions to be executed as part of the Kubernetes
/// Pod lifecycle, such as pre-stop and post-start events. It supports multiple
/// action types, including executing a command, sleeping for a duration, making
/// HTTP GET requests, or establishing TCP socket connections.
/// </remarks>
[YamlSerializable]
public sealed class LifecycleHandlerV1
{
    /// <summary>
    /// Represents an action that executes a command within a container.
    /// </summary>
    /// <remarks>
    /// This property defines the execution of a command line inside a container. It is typically used in
    /// Kubernetes lifecycle hooks or probes for custom actions such as health checks or running scripts.
    /// </remarks>
    [YamlMember(Alias = "exec")]
    public ExecActionV1 Exec { get; set; } = new();

    /// <summary>
    /// Gets or sets a sleep action configuration for a lifecycle handler.
    /// </summary>
    /// <remarks>
    /// Sleep specifies a delay for a defined duration in seconds. This is commonly
    /// used in Kubernetes lifecycle management to introduce a delay before
    /// proceeding to the next operation.
    /// </remarks>
    [YamlMember(Alias = "sleep")]
    public SleepActionV1 Sleep { get; set; } = new();

    /// <summary>
    /// Represents the HTTP GET action associated with a lifecycle handler in Kubernetes resources.
    /// </summary>
    /// <remarks>
    /// The action defines an HTTP GET request that is typically utilized in scenarios such as health checks
    /// or readiness probes. It supports configuration of parameters including the HTTP scheme, target path,
    /// host, port, and optional HTTP headers.
    /// </remarks>
    [YamlMember(Alias = "httpGet")]
    public HttpGetActionV1 HttpGet { get; set; } = new();

    /// <summary>
    /// Gets or sets the TcpSocketActionV1 property.
    /// </summary>
    /// <remarks>
    /// TcpSocket represents an action performed on a specified TCP socket.
    /// This property is used in the context of lifecycle hooks or probes
    /// within Kubernetes resources to establish a connection on the given host and port.
    /// </remarks>
    [YamlMember(Alias = "tcpSocket")]
    public TcpSocketActionV1 TcpSocket { get; set; } = new();
}
