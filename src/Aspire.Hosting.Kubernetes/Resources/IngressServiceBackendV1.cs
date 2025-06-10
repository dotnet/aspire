// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a backend service configuration in a Kubernetes ingress resource.
/// </summary>
/// <remarks>
/// This class defines the service-based backend for routing traffic in Kubernetes Ingress configurations.
/// It specifies the service name and the port of the service that will handle incoming traffic.
/// </remarks>
[YamlSerializable]
public sealed class IngressServiceBackendV1
{
    /// <summary>
    /// Gets or sets the name of the referenced Kubernetes service.
    /// Represents the identifier of the service that the backend points to.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Represents the port information for a backend service within an ingress resource.
    /// This property defines the port details as a <see cref="ServiceBackendPortV1"/> object,
    /// which includes the port number and an optional name associated with the port.
    /// </summary>
    [YamlMember(Alias = "port")]
    public ServiceBackendPortV1 Port { get; set; } = new();
}
