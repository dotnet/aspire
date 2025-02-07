// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery, IResourceWithWaitSupport
{
    // Keep track of the config host for each Kestrel endpoint annotation
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    internal bool ShouldInjectEndpointEnvironment(EndpointReference e)
    {
        var endpoint = e.EndpointAnnotation;

        if (endpoint.UriScheme is not ("http" or "https") ||    // Only process http and https endpoints
            endpoint.TargetPortEnvironmentVariable is not null) // Skip if target port env variable was set
        {
            return false;
        }

        // If any filter rejects the endpoint, skip it
        return !Annotations.OfType<EndpointEnvironmentInjectionFilterAnnotation>()
                           .Select(a => a.Filter)
                           .Any(f => !f(endpoint));
    }
}
