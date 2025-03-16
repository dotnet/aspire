// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// GRPCActionV1 represents a GRPC-based action within a Kubernetes resource.
/// </summary>
/// <remarks>
/// This action defines a GRPC operation by specifying the target service and port.
/// It is typically used in Kubernetes probes to perform health checks and ensure
/// the connectivity and functionality of a GRPC service within a containerized environment.
/// </remarks>
[YamlSerializable]
public sealed class GrpcActionV1
{
    /// <summary>
    /// Gets or sets the name of the GRPC service to be targeted.
    /// </summary>
    [YamlMember(Alias = "service")]
    public string Service { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port number used for the gRPC service communication.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int Port { get; set; }
}
