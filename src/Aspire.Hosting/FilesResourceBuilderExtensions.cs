// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding files resources to an application.
/// </summary>
public static class FilesResourceBuilderExtensions
{
    /// <summary>
    /// Adds a files resource to the application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the files resource.</param>
    /// <returns>A resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> AddFiles(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var filesResource = new FilesResource(name, []);
        return builder.AddResource(filesResource);
    }

    /// <summary>
    /// Adds a source directory or file to an existing files resource.
    /// </summary>
    /// <param name="builder">The resource builder for the files resource.</param>
    /// <param name="source">The source path (directory or file) to associate with this resource.</param>
    /// <returns>The resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> WithSource(this IResourceBuilder<FilesResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.Resource.AddFile(source);
        return builder;
    }
}