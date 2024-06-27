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

    // Filter that determines if we should inject environment variables for a given endpoint.
    // By default, we do it for all endpoints, but users can override it.
    EnvironmentInjectionFilterEntry EnvironmentFilterEntry { get; set; } = new();

    // Add a new filter to the chain (creating a linked list)
    internal void AddEnvironmentInjectionFilter(Func<EndpointAnnotation, bool> filter)
    {
        EnvironmentFilterEntry = new EnvironmentInjectionFilterEntry
        {
            Filter = filter,
            Previous = EnvironmentFilterEntry
        };
    }

    internal bool ShouldInjectEndpointEnvironment(EndpointReference e) => EnvironmentFilterEntry.ShouldInject(e.EndpointAnnotation);

    class EnvironmentInjectionFilterEntry
    {
        // Set a default filter that excludes some endpoints from environment injection
        public Func<EndpointAnnotation, bool> Filter { get; set; } = e =>
            e.UriScheme is "http" or "https" &&         // Only process http and https endpoints
            e.TargetPortEnvironmentVariable is null;    // Skip if target port env variable was set

        public EnvironmentInjectionFilterEntry? Previous { get; set; }

        // Recursively combine the current filter with its parent (if any)
        internal bool ShouldInject(EndpointAnnotation e) => (Previous == null || Previous.ShouldInject(e)) && Filter(e);
    }
}
