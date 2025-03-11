// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents resource specifications for a Docker service.
/// </summary>
/// <remarks>
/// The <c>ResourceSpec</c> class is used to define constraints on CPU and memory
/// for a service in the Docker Swarm mode. These configurations can be applied
/// to set limits or reservations for resources.
/// </remarks>
[YamlSerializable]
public sealed class ResourceSpec
{
    /// <summary>
    /// Represents the amount of CPU resources allocated for a specific service node in the swarm configuration.
    /// The value is typically expressed in terms of CPU shares or fractions, depending on the context.
    /// </summary>
    [YamlMember(Alias = "cpus")]
    public string? Cpus { get; set; }

    /// <summary>
    /// Represents the memory resource specification for the service.
    /// This property defines the amount of memory allocated as a resource.
    /// The value is typically specified as a string representing the size,
    /// for example "512M" for 512 megabytes.
    /// </summary>
    [YamlMember(Alias = "memory")]
    public string? Memory { get; set; }
}
