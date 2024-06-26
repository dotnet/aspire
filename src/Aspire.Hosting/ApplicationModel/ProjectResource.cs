// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
{
    // Keep track of the config host for each Kestrel endpoint annotation
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    // Keep track of the endpoints for which we should not inject environment variables
    private readonly HashSet<EndpointAnnotation> _endpointsWithoutEnv = new();
    internal void SkipEndpointEnvironment(EndpointAnnotation endpoint)
    {
        _endpointsWithoutEnv.Add(endpoint);
    }
    internal bool ShouldSkipEndpointEnvironment(EndpointAnnotation endpoint) => _endpointsWithoutEnv.Contains(endpoint);
}
