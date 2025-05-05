// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a port configuration for a Kubernetes Service in API version V1.
/// </summary>
/// <remarks>
/// This class is used to configure the port settings for a Kubernetes Service.
/// It includes details used to define how the Service exposes ports and communicates with underlying pods.
/// </remarks>
[YamlSerializable]
public sealed class ServicePortV1
{
    /// <summary>
    /// Gets or sets the name of the service port. This is an optional identifier
    /// that can be used to distinguish between ports in cases where there are multiple
    /// ports defined. It must be a unique string within the service if specified.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the application protocol associated with the service port.
    /// This property can be used to define custom application layer protocols
    /// (e.g., HTTP, HTTPS) for traffic management and service communication.
    /// </summary>
    [YamlMember(Alias = "appProtocol")]
    public string AppProtocol { get; set; } = null!;

    /// <summary>
    /// Gets or sets the protocol used by the service, such as "TCP" or "UDP".
    /// </summary>
    [YamlMember(Alias = "protocol")]
    public string Protocol { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port on the node where the service is exposed.
    /// This is used for NodePort services to specify the static port number
    /// on which the service is accessible on each selected node. If not set,
    /// a port will be automatically assigned from the NodePort range determined
    /// by the system.
    /// </summary>
    [YamlMember(Alias = "nodePort")]
    public int? NodePort { get; set; }

    /// <summary>
    /// Represents the port number that the service will expose.
    /// This value specifies the port on which the service is accessible.
    /// </summary>
    [YamlMember(Alias = "port")]
    public Int32OrStringV1 Port { get; set; } = null!;

    /// <summary>
    /// Specifies the port on the target container to which traffic should be directed.
    /// Typically used in Kubernetes Service definitions to map incoming traffic to the appropriate port of the application running in a pod.
    /// </summary>
    [YamlMember(Alias = "targetPort")]
    public Int32OrStringV1 TargetPort { get; set; } = null!;
}
