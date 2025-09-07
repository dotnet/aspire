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
    /// <param name="files">The collection of file paths to associate with this resource.</param>
    /// <returns>A resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> AddFiles(this IDistributedApplicationBuilder builder, [ResourceName] string name, IEnumerable<string> files)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(files);

        var filesResource = new FilesResource(name, files);
        return builder.AddResource(filesResource);
    }

    /// <summary>
    /// Adds a files resource to the application with a single file.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the files resource.</param>
    /// <param name="filePath">The file path to associate with this resource.</param>
    /// <returns>A resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> AddFiles(this IDistributedApplicationBuilder builder, [ResourceName] string name, string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(filePath);

        return builder.AddFiles(name, [filePath]);
    }

    /// <summary>
    /// Adds a files resource to the application without any initial files.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the files resource.</param>
    /// <returns>A resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> AddFiles(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.AddFiles(name, []);
    }

    /// <summary>
    /// Adds a file to an existing files resource.
    /// </summary>
    /// <param name="builder">The resource builder for the files resource.</param>
    /// <param name="filePath">The file path to add.</param>
    /// <returns>The resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> WithFile(this IResourceBuilder<FilesResource> builder, string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(filePath);

        builder.Resource.AddFile(filePath);
        return builder;
    }

    /// <summary>
    /// Adds multiple files to an existing files resource.
    /// </summary>
    /// <param name="builder">The resource builder for the files resource.</param>
    /// <param name="filePaths">The file paths to add.</param>
    /// <returns>The resource builder for the files resource.</returns>
    public static IResourceBuilder<FilesResource> WithFiles(this IResourceBuilder<FilesResource> builder, IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(filePaths);

        builder.Resource.AddFiles(filePaths);
        return builder;
    }
}