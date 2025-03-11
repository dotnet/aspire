// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents the resource configurations for a Docker service in Swarm mode.
/// </summary>
/// <remarks>
/// The <c>Resources</c> class defines the optional constraints for resource allocation
/// by specifying limits and reservations. Limits define the maximum resources
/// a service can utilize, while reservations define the minimum guaranteed resources.
/// </remarks>
[YamlSerializable]
public sealed class Resources
{
    /// <summary>
    /// Gets or sets the resource limits for a Docker service.
    /// </summary>
    /// <remarks>
    /// The <c>Limits</c> property defines the maximum resources, such as CPU and memory,
    /// that can be allocated to a Docker service. It is used to restrict the usage
    /// of system resources by the service within specified boundaries.
    /// </remarks>
    [YamlMember(Alias = "limits")]
    public ResourceSpec? Limits { get; set; }

    /// <summary>
    /// Gets or sets the resources reserved for a Docker service in Swarm mode.
    /// </summary>
    /// <remarks>
    /// This property represents the resource reservations for a Docker service. These reservations ensure
    /// that the specified resources, such as CPU and memory, are allocated to the service when it runs.
    /// Resource reservations help in providing guaranteed resource availability to the service within
    /// the cluster.
    /// </remarks>
    [YamlMember(Alias = "reservations")]
    public ResourceSpec? Reservations { get; set; }
}
