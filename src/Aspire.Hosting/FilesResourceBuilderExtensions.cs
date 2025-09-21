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

        // Add the source immediately to maintain existing behavior
        builder.Resource.AddFile(source);

        // Add callback annotation that will enumerate files from the source
        builder.WithAnnotation(new FilesCallbackAnnotation(cancellationToken => EnumerateFilesAsync(source, cancellationToken)));

        return builder;
    }

    private static async Task<IEnumerable<ResourceFile>> EnumerateFilesAsync(string source, CancellationToken cancellationToken = default)
    {
        var files = new List<ResourceFile>();
        
        // Get the absolute path for consistent path calculations
        var absoluteSource = Path.GetFullPath(source);
        
        // If the source is a directory, enumerate all files in it
        if (Directory.Exists(absoluteSource))
        {
            // Return the directory itself as a resource file
            var directoryRelativePath = Path.GetFileName(absoluteSource);
            files.Add(new ResourceFile(absoluteSource, directoryRelativePath));
            
            var directoryFiles = Directory.GetFiles(absoluteSource, "*", SearchOption.AllDirectories);
            foreach (var file in directoryFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Calculate relative path from the source directory
                var relativePath = Path.GetRelativePath(absoluteSource, file);
                // Normalize path separators
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                
                files.Add(new ResourceFile(file, relativePath));
            }
        }
        else if (File.Exists(absoluteSource))
        {
            // If it's a file, just return it with its filename as relative path
            var fileName = Path.GetFileName(absoluteSource);
            files.Add(new ResourceFile(absoluteSource, fileName));
        }
        
        await Task.CompletedTask.ConfigureAwait(false); // Satisfy async requirements
        return files;
    }
}