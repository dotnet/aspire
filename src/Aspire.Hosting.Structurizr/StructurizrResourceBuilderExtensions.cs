// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Structurizr server resources to the application model.
/// </summary>
public static class StructurizrResourceBuilderExtensions
{
    // The path within the container in which Seq stores its data
    const string StructurizrContainerDataDirectory = "/usr/local/structurizr";

    /// <summary>
    /// Adds a Structurizr server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name to give the resource.</param>
    /// <param name="httpPort">The host port for the Structurizr server.</param>
    /// <param name="bindPath">The path on which the Structurizr will save the files on the host; if not provided it will use <see cref="Directory.GetCurrentDirectory"/>.</param>
#pragma warning disable RS0016 // Add public types and members to the declared API
    public static IResourceBuilder<StructurizrResource> AddStructurizr(
#pragma warning restore RS0016 // Add public types and members to the declared API
        this IDistributedApplicationBuilder builder, string name, int httpPort = StructurizrResource.DefaultPortNumber,
        string? bindPath = null)
    {
        var resource = new StructurizrResource(name);

        return builder.AddResource(resource)
                .WithImage(StructurizrContainerImageTags.Image)
                .WithImageRegistry(StructurizrContainerImageTags.Registry)
                .WithImageTag(StructurizrContainerImageTags.Tag)
                .WithHttpEndpoint(port: httpPort, targetPort: StructurizrResource.DefaultPortNumber,
                    name: StructurizrResource.HttpEndpointName)
                .WithBindMount(bindPath ?? Directory.GetCurrentDirectory(), StructurizrContainerDataDirectory)
            ;
    }
}
