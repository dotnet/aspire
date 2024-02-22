// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string expression for the Redis server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return $"{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}";
        }
    }

    /// <summary>
    /// Gets the connection string for the Redis server.
    /// </summary>
    /// <returns>A connection string for the redis server in the form "host:port".</returns>
    public string? GetConnectionString()
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionString();
        }

        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Redis resource does not have endpoint annotation.");
        }

        // We should only have one endpoint for Redis for local scenarios.
        var endpoint = allocatedEndpoints.Single();
        return endpoint.EndPointString;
    }
}
