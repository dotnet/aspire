// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
public interface IResourceContainerImageBuilder
{
    /// <summary>
    /// Builds a container that represents the specified resource.
    /// </summary>
    /// <param name="resource">The resource to build.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>String representing the container image.</returns>
    Task<string> BuildImageAsync(IResource resource, CancellationToken cancellationToken);
}

internal sealed class ResourceContainerImageBuilder(ILogger<ResourceContainerImageBuilder> logger, IOptions<DcpOptions> dcpOptions, IServiceProvider serviceProvider) : IResourceContainerImageBuilder
{
    public async Task<string> BuildImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building container image for resource {Resource}", resource.Name);

        if (resource is ProjectResource)
        {
            // This is a resource project so we'll use the .NET SDK to build the container image.
            var image = await BuildProjectContainerImageAsync(resource, cancellationToken).ConfigureAwait(false);
            return image;
        }
        else if (resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var contaimerImageAnnotation))
        {
            if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerfileBuildAnnotation))
            {
                // This is a container resource so we'll use the container runtime to build the image
                return await BuildContainerImageFromDockerfileAsync(
                    dockerfileBuildAnnotation.ContextPath,
                    dockerfileBuildAnnotation.DockerfilePath,
                    contaimerImageAnnotation.Image,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // ... except in this case where there is nothing to build, so we just return the image name.
                return contaimerImageAnnotation.Image;
            }
        }
        else
        {
            throw new NotSupportedException($"The resource type '{resource.GetType().Name}' is not supported.");
        }
    }

    private async Task<string> BuildProjectContainerImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
        {
            throw new DistributedApplicationException($"The resource '{projectMetadata}' does not have a project metadata annotation.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(projectMetadata.ProjectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("/t:PublishContainer");
        startInfo.ArgumentList.Add($"/p:ContainerRepository={resource.Name}");

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new DistributedApplicationException("Failed to start .NET CLI.");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();

            logger.LogError(
                ".NET CLI failed with exit code {ExitCode}. Output: {Stdout}, Error: {Stderr}",
                process.ExitCode,
                stdout,
                stderr);

            throw new DistributedApplicationException("Failed to build container image.");
        }
        else
        {
            return $"{resource.Name}:latest";
        }
    }

    private async Task<string> BuildContainerImageFromDockerfileAsync(string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        var containerRuntime = dcpOptions.Value.ContainerRuntime switch
        {
            "podman" => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("podman"),
            _ => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("docker")
        };

        var image = await containerRuntime.BuildImageAsync(
            contextPath,
            dockerfilePath,
            imageName,
            cancellationToken).ConfigureAwait(false);

        return image;
    }
}