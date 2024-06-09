// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
{
    // Track endpoints came from Kestrel configuration
    internal HashSet<EndpointAnnotation> KestrelEndpointAnnotations { get; } = new();

    // Track the https endpoint that was added as a default, and should be excluded from the port environment
    internal EndpointAnnotation? EndpointExcludedFromPortEnvironment { get; set; }
}
