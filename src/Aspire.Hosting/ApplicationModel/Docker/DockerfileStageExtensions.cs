// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
    /// <returns>The <see cref="DockerfileStage"/> to allow for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method processes all <see cref="ContainerFilesDestinationAnnotation"/> annotations on the resource
    /// and generates COPY --from statements for each source container's files.
    /// </para>
    /// <para>
    /// For each annotation:
    /// <list type="bullet">
    /// <item>The source resource must have a container image name (via <c>TryGetContainerImageName</c>)</item>
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
    /// <example>
    /// <code lang="csharp">
    /// var runtimeStage = dockerfileBuilder
    ///     .From("mcr.microsoft.com/dotnet/aspnet:8.0", "runtime")
    ///     .WorkDir("/app")
    ///     .CopyFrom("build", "/app", "/app")
    ///     .AddContainerFiles(resource, "/app")  // Adds files from referenced containers
    ///     .Entrypoint(["dotnet", "MyApp.dll"]);
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a source resource does not have a container image name.
    /// </exception>
    [Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static DockerfileStage AddContainerFiles(this DockerfileStage stage, IResource resource, string rootDestinationPath)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(rootDestinationPath);

        if (resource.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesDestinationAnnotations))
        {
            foreach (var containerFileDestination in containerFilesDestinationAnnotations)
            {
                // get image name
                if (!containerFileDestination.Source.TryGetContainerImageName(out var imageName))
                {
                    throw new InvalidOperationException("Cannot add container files: Source resource does not have a container image name.");
                }

                var destinationPath = containerFileDestination.DestinationPath;
                if (!destinationPath.StartsWith('/'))
                {
                    destinationPath = $"{rootDestinationPath}/{destinationPath}";
                }

                foreach (var containerFilesSource in containerFileDestination.Source.Annotations.OfType<ContainerFilesSourceAnnotation>())
                {
                    stage.CopyFrom(imageName, containerFilesSource.SourcePath, destinationPath);
                }
            }

            stage.EmptyLine();
        }
        return stage;
    }
}
