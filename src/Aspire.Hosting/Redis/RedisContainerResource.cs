// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

public class RedisContainerResource(string name) : ContainerResource(name), IRedisResource
{
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
