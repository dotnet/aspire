// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
{
    // Map endpoint annotations to the kestrel config hosts we created them for
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    internal bool IsDefaultEndpoint(EndpointAnnotation endpoint)
    {
        // Determine if the endpoint should be treated as the Default endpoint.
        // Endpoints can come from 3 different sources (in this order):
        // 1. Kestrel configuration
        // 2. Default endpoints added by the framework
        // 3. Explicitly added endpoints
        // But wherever they come from, we treat the first one as Default (for each scheme).
        // NOTE: the implementation is a bit inefficient as it iterates over the endpoints
        // for each check, but it's not a performance critical path.
        return endpoint == Annotations.OfType<EndpointAnnotation>().FirstOrDefault(
            e => e.UriScheme == endpoint.UriScheme);
    }
}
