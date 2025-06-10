// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a service backend port configuration in Kubernetes.
/// </summary>
/// <remarks>
/// This class defines the port information for a Kubernetes service, which can be used
/// in configurations like ingress services or other Kubernetes networking components.
/// The port may be specified by name or number, enabling flexibility for different scenarios.
/// </remarks>
[YamlSerializable]
public sealed class ServiceBackendPortV1
{
    /// <summary>
    /// Gets or sets the name of the backend port. This is used to specify the name
    /// of the port when referring to it in a service configuration.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Represents the numeric port value for the service backend.
    /// Used to specify the numeric port on the service to which traffic is directed.
    /// </summary>
    [YamlMember(Alias = "number")]
    public int? Number { get; set; }
}
