// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public class MongoDBContainerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    public string Password { get; } = password;

    public string? GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var endpoint = allocatedEndpoints.Single();

        return $"mongodb://host.docker.internal:{endpoint.Port}";
    }
}
