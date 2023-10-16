// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RabbitMQ;

public class RabbitMQContainerResource(string name, string? password) : ContainerResource(name), IDistributedApplicationResourceWithConnectionString
{
    public string? Password { get; } = password;

    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"RabbitMQ resource \"{Name}\" does not have endpoint annotation.");
        }

        var endpoint = allocatedEndpoints.Where(a => a.Name != "management").Single();
        if (Password is null)
        {
            return $"amqp://{endpoint.EndPointString}";
        }

        return $"amqp://guest:{Password}@{endpoint.EndPointString}";
    }
}
