// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Minio server.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MinioServerResource(string name) : Resource(name), IResourceWithConnectionString, IResourceWithEnvironment
{
    /// <summary>
    /// Gets the connection string for the Minio server.
    /// </summary>
    /// <returns>A connection string for the Minio server in the form "http://host:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"Minio resource \"{Name}\" does not have endpoint annotation.");
        }

        // Assuming Minio runs on HTTP by default. Adjust if it uses HTTPS.
        var endpoint = allocatedEndpoints.SingleOrDefault();
        return endpoint != null ? $"http://{endpoint.EndPointString}" : null;
    }

    /// <summary>
    /// Gets the service port for the Minio server.
    /// </summary>
    /// <returns>The service port used by the Minio server.</returns>
    public int? GetServicePort()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"Minio resource \"{Name}\" does not have endpoint annotation.");
        }

        return allocatedEndpoints.SingleOrDefault()?.Port;
    }
}
