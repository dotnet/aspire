// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Keycloak container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class KeycloakContainerResource(string name) : ContainerResource(name), IResourceWithEnvironment, IResourceWithServiceDiscovery
{

    /// <summary>
    /// Gets the endpoint string for the Keycloak server.
    /// </summary>
    /// <returns>A endpoint string for the Keycloak server in the form "host:port".</returns>
    public string? GetEndpoints()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint.

        var endpoint = $"{allocatedEndpoint.Address}:{allocatedEndpoint.Port}";
        return endpoint;
    }
}
