// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Provides extension methods for <see cref="DockerfileStage"/>.
/// </summary>
public static class DockerfileStageExtensions
{
    /// <summary>
    /// Adds COPY --from statements to the Dockerfile stage for container files from resources referenced by <see cref="ContainerFilesDestinationAnnotation"/>.
    /// </summary>
    /// <param name="stage">The Dockerfile stage to add container file copy statements to.</param>
    /// <param name="resource">The resource that may have <see cref="ContainerFilesDestinationAnnotation"/> annotations specifying files to copy.</param>
    /// <param name="rootDestinationPath">The root destination path in the container. Relative paths in annotations will be appended to this path.</param>
    /// <param name="logger">The logger used for logging information or errors.</param>
    /// <returns>The <see cref="DockerfileStage"/> to allow for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method processes all <see cref="ContainerFilesDestinationAnnotation"/> annotations on the resource
    /// and generates COPY --from statements for each source container's files.
    /// </para>
    /// <para>
    /// For each annotation:
    /// <list type="bullet">
    /// <item>If the source resource has a container image name (via <c>TryGetContainerImageName</c>), COPY statements are generated</item>
    /// <item>If the source resource does not have a container image name, it is silently skipped</item>
    /// <item>Relative destination paths are combined with <paramref name="rootDestinationPath"/></item>
    /// <item>Absolute destination paths are used as-is</item>
    /// <item>Each <see cref="ContainerFilesSourceAnnotation"/> on the source resource generates a COPY statement</item>
    /// </list>
    /// </para>
    /// <para>
    /// This is typically used when building container images that need to include files from other containers,
    /// such as copying static assets from a frontend build container into a backend API container.
    /// </para>
    /// </remarks>
    [Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static DockerfileStage AddContainerFiles(this DockerfileStage stage, IResource resource, string rootDestinationPath, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(rootDestinationPath);

        if (resource.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesDestinationAnnotations))
        {
            foreach (var containerFileDestination in containerFilesDestinationAnnotations)
            {
                var source = containerFileDestination.Source;

                // get image name - skip this source if it doesn't have an image name
                if (!source.TryGetContainerImageName(out var sourceImageName))
                {
                    logger?.LogWarning("Cannot get container image name for source resource {SourceName}, skipping", source.Name);
                    continue;
                }

                var destinationPath = containerFileDestination.DestinationPath;
                if (!destinationPath.StartsWith('/'))
                {
                    destinationPath = $"{rootDestinationPath}/{destinationPath}";
                }

                foreach (var containerFilesSource in source.Annotations.OfType<ContainerFilesSourceAnnotation>())
                {
                    logger?.LogDebug("Adding COPY --from={SourceImage} {SourcePath} {DestinationPath}",
                        sourceImageName, containerFilesSource.SourcePath, destinationPath);
                    stage.CopyFrom(sourceImageName, containerFilesSource.SourcePath, destinationPath);
                }
            }

            stage.EmptyLine();
        }
        return stage;
    }
}
