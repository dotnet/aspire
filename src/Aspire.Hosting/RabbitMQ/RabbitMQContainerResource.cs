// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The RabbitMQ server password.</param>
public class RabbitMQContainerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// The RabbitMQ server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string for the RabbitMQ server.
    /// </summary>
    /// <returns>A connection string for the RabbitMQ server in the form "amqp://user:password@host:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"RabbitMQ resource \"{Name}\" does not have endpoint annotation.");
        }

        var endpoint = allocatedEndpoints.Where(a => a.Name != "management").Single();
        return $"amqp://guest:{Password}@{endpoint.EndPointString}";
    }
}
