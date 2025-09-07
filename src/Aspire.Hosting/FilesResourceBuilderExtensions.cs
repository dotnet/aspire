// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

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

        // Add source annotation to track the source path for validation during initialization
        builder.WithAnnotation(new FilesSourceAnnotation(source));

        // Check if we've already registered the initialization handler for this resource
        var handlerRegistered = builder.Resource.Annotations.OfType<FilesInitializationHandlerRegisteredAnnotation>().Any();
        
        if (!handlerRegistered)
        {
            // Mark that we've registered the handler
            builder.WithAnnotation(new FilesInitializationHandlerRegisteredAnnotation());

            // Subscribe to the InitializeResourceEvent to process the source when the resource is initialized
            builder.OnInitializeResource(async (filesResource, initEvent, ct) =>
            {
                var sourceAnnotations = filesResource.Annotations.OfType<FilesSourceAnnotation>();
                var validatedFiles = new List<string>();

                foreach (var sourceAnnotation in sourceAnnotations)
                {
                    var sourcePath = sourceAnnotation.Source;
                    
                    // Verify that the path specified in WithSource(path) is a valid directory
                    if (Directory.Exists(sourcePath))
                    {
                        validatedFiles.Add(sourcePath);
                        // Add all files within the directory to the validated list
                        var filesInDirectory = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                        validatedFiles.AddRange(filesInDirectory);
                    }
                    else
                    {
                        initEvent.Logger.LogWarning("Source path '{SourcePath}' for files resource '{ResourceName}' is not a valid directory.", sourcePath, filesResource.Name);
                        continue;
                    }
                }

                // Fire the FilesProducedEvent with the validated files
                if (validatedFiles.Count > 0)
                {
                    await initEvent.Eventing.PublishAsync(new FilesProducedEvent(filesResource, initEvent.Services, validatedFiles), ct).ConfigureAwait(false);
                }

                // Fire the ResourceReadyEvent
                await initEvent.Eventing.PublishAsync(new ResourceReadyEvent(filesResource, initEvent.Services), ct).ConfigureAwait(false);
            });
        }

        return builder;
    }
}