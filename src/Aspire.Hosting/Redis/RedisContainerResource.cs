// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisContainerResource(string name) : ContainerResource(name), IRedisResource
{
    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <returns>A connection string for the redis server in the form "host:port".</returns>
    public string GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Redis resource does not have endpoint annotation.");
        }

        // We should only have one endpoint for Redis for local scenarios.
        var endpoint = allocatedEndpoints.Single();
        return endpoint.EndPointString;
    }
}
