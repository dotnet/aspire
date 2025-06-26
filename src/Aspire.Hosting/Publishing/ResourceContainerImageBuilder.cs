// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Specifies the format for container images.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum ContainerImageFormat
{
    /// <summary>
    /// Docker format (default).
    /// </summary>
    Docker,

    /// <summary>
    /// OCI tar format.
    /// </summary>
    OciTar,

    /// <summary>
    /// Docker tar format.
    /// </summary>
    DockerTar
}

/// <summary>
/// Options for building container images.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ContainerBuildOptions
{
    /// <summary>
    /// Gets or sets the output path for the container archive.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets or sets the container image format.
    /// </summary>
    public ContainerImageFormat? ImageFormat { get; init; }

    /// <summary>
    /// Gets or sets the target platform for the container.
    /// </summary>
    public string? TargetPlatform { get; init; }
}

/// <summary>
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IResourceContainerImageBuilder
{
    /// <summary>
    /// Builds a container that represents the specified resource.
    /// </summary>
    /// <param name="resource">The resource to build.</param>
    /// <param name="options">The container build options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BuildImageAsync(IResource resource, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds container images for a collection of resources.
    /// </summary>
    /// <param name="resources">The resources to build images for.</param>
    /// <param name="options">The container build options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task BuildImagesAsync(IEnumerable<IResource> resources, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default);
}

internal sealed class ResourceContainerImageBuilder(
    ILogger<ResourceContainerImageBuilder> logger,
    IOptions<DcpOptions> dcpOptions,
    IServiceProvider serviceProvider,
    IPublishingActivityReporter activityReporter) : IResourceContainerImageBuilder
{
    private IContainerRuntime? _containerRuntime;
    private IContainerRuntime ContainerRuntime => _containerRuntime ??= dcpOptions.Value.ContainerRuntime switch
    {
        string rt => serviceProvider.GetRequiredKeyedService<IContainerRuntime>(rt),
        null => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("docker")
    };

    public async Task BuildImagesAsync(IEnumerable<IResource> resources, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default)
    {
        var step = await activityReporter.CreateStepAsync(
            "Building container images for resources",
            cancellationToken).ConfigureAwait(false);

        await using (step.ConfigureAwait(false))
        {
            // Currently, we build these images to the local Docker daemon. We need to ensure that
            // the Docker daemon is running and accessible

            var task = await step.CreateTaskAsync(
                $"Checking {ContainerRuntime.Name} health",
                cancellationToken).ConfigureAwait(false);

            await using (task.ConfigureAwait(false))
            {
                var containerRuntimeHealthy = await ContainerRuntime.CheckIfRunningAsync(cancellationToken).ConfigureAwait(false);

                if (!containerRuntimeHealthy)
                {
                    logger.LogError("Container runtime is not running or is unhealthy. Cannot build container images.");

                    await task.FailAsync(
                        $"{ContainerRuntime.Name} is not running or is unhealthy.",
                        cancellationToken).ConfigureAwait(false);

                    await step.CompleteAsync("Building container images failed", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                    return;
                }

                await task.SucceedAsync(
                    $"{ContainerRuntime.Name} is healthy.",
                    cancellationToken).ConfigureAwait(false);
            }

            foreach (var resource in resources)
            {
                // TODO: Consider parallelizing this.
                await BuildImageAsync(step, resource, options, cancellationToken).ConfigureAwait(false);
            }

            await step.CompleteAsync("Building container images completed", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task BuildImageAsync(IResource resource, ContainerBuildOptions? options = null, CancellationToken cancellationToken = default)
    {
        return BuildImageAsync(step: null, resource, options, cancellationToken);
    }

    // Backward compatibility methods
    public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken)
    {
        return BuildImagesAsync(resources, options: null, cancellationToken);
    }

    public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        return BuildImageAsync(resource, options: null, cancellationToken);
    }

    private async Task BuildImageAsync(PublishingStep? step, IResource resource, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building container image for resource {Resource}", resource.Name);

        if (resource is ProjectResource)
        {
            // If it is a project resource we need to build the container image
            // using the .NET SDK.
            await BuildProjectContainerImageAsync(
                resource,
                step,
                options,
                cancellationToken).ConfigureAwait(false);
            return;
        }
        else if (resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation))
        {
            if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerfileBuildAnnotation))
            {
                // This is a container resource so we'll use the container runtime to build the image
                await BuildContainerImageFromDockerfileAsync(
                    resource.Name,
                    dockerfileBuildAnnotation.ContextPath,
                    dockerfileBuildAnnotation.DockerfilePath,
                    containerImageAnnotation.Image,
                    step,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        else
        {
            throw new NotSupportedException($"The resource type '{resource.GetType().Name}' is not supported.");
        }
    }

    private async Task BuildProjectContainerImageAsync(IResource resource, IPublishingStep? step, ContainerBuildOptions? options, CancellationToken cancellationToken)
    {
        var publishingTask = await CreateTaskAsync(
            step,
            $"Building image: {resource.Name}",
            cancellationToken
            ).ConfigureAwait(false);

        if (publishingTask is not null)
        {
            await using (publishingTask.ConfigureAwait(false))
            {
                // This is a resource project so we'll use the .NET SDK to build the container image.
                if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
                {
                    throw new DistributedApplicationException($"The resource '{projectMetadata}' does not have a project metadata annotation.");
                }

                var spec = new ProcessSpec("dotnet")
                {
                    Arguments = $"publish {projectMetadata.ProjectPath} --configuration Release /t:PublishContainer /p:ContainerRepository={resource.Name}",
                    OnOutputData = output =>
                    {
                        logger.LogInformation("dotnet publish {ProjectPath} (stdout): {Output}", projectMetadata.ProjectPath, output);
                    },
                    OnErrorData = error =>
                    {
                        logger.LogError("dotnet publish {ProjectPath} (stderr): {Error}", projectMetadata.ProjectPath, error);
                    }
                };

                logger.LogInformation(
                    "Starting .NET CLI with arguments: {Arguments}",
                    string.Join(" ", spec.Arguments)
                    );

                var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

                await using (processDisposable)
                {
                    var processResult = await pendingProcessResult
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (processResult.ExitCode != 0)
                    {
                        logger.LogError("dotnet publish for project {ProjectPath} failed with exit code {ExitCode}.", projectMetadata.ProjectPath, processResult.ExitCode);

                        await publishingTask.FailAsync($"Building image for {resource.Name} failed", cancellationToken).ConfigureAwait(false);
                        throw new DistributedApplicationException($"Failed to build container image.");
                    }
                    else
                    {
                        await publishingTask.SucceedAsync($"Building image for {resource.Name} completed", cancellationToken).ConfigureAwait(false);

                        logger.LogDebug(
                            ".NET CLI completed with exit code: {ExitCode}",
                            processResult.ExitCode);
                    }
                }
            }
        }

        // Build the dotnet publish command with base arguments
        var arguments = $"publish {projectMetadata.ProjectPath} --configuration Release /t:PublishContainer /p:ContainerRepository={resource.Name}";

        // Add additional arguments based on options
        if (options is not null)
        {
            if (!string.IsNullOrEmpty(options.OutputPath))
            {
                arguments += $" /p:ContainerArchiveOutputPath={options.OutputPath}";
            }

            if (options.ImageFormat.HasValue)
            {
                var format = options.ImageFormat.Value switch
                {
                    ContainerImageFormat.Docker => "Docker",
                    ContainerImageFormat.OciTar => "OciTar",
                    ContainerImageFormat.DockerTar => "DockerTar",
                    _ => throw new ArgumentOutOfRangeException(nameof(options), options.ImageFormat, "Invalid container image format")
                };
                arguments += $" /p:ContainerImageFormat={format}";
            }

            if (!string.IsNullOrEmpty(options.TargetPlatform))
            {
                arguments += $" /p:ContainerRuntimeIdentifier={options.TargetPlatform}";
            }
        }

        var spec = new ProcessSpec("dotnet")
        {
            Arguments = $"publish {projectMetadata.ProjectPath} --configuration Release /t:PublishContainer /p:ContainerRepository={resource.Name}",
            OnOutputData = output =>
            {
                logger.LogInformation("dotnet publish {ProjectPath} (stdout): {Output}", projectMetadata.ProjectPath, output);
            },
            OnErrorData = error =>
            {
                logger.LogError("dotnet publish {ProjectPath} (stderr): {Error}", projectMetadata.ProjectPath, error);
            }
        };

        logger.LogInformation(
            "Starting .NET CLI with arguments: {Arguments}",
            string.Join(" ", spec.Arguments)
            );

        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                logger.LogError("dotnet publish for project {ProjectPath} failed with exit code {ExitCode}.", projectMetadata.ProjectPath, processResult.ExitCode);
                throw new DistributedApplicationException($"Failed to build container image.");
            }
            else
            {
                logger.LogDebug(
                    ".NET CLI completed with exit code: {ExitCode}",
                    processResult.ExitCode);
            }
        }
    }

    private async Task BuildContainerImageFromDockerfileAsync(string resourceName, string contextPath, string dockerfilePath, string imageName, IPublishingStep? step, CancellationToken cancellationToken)
    {
        var publishingTask = await CreateTaskAsync(
            step,
            $"Building image: {resourceName}",
            cancellationToken
            ).ConfigureAwait(false);

        if (publishingTask is not null)
        {
            await using (publishingTask.ConfigureAwait(false))
            {
                try
                {
                    await ContainerRuntime.BuildImageAsync(
                        contextPath,
                        dockerfilePath,
                        imageName,
                        cancellationToken).ConfigureAwait(false);

                    await publishingTask.SucceedAsync($"Building image for {resourceName} completed", cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to build container image from Dockerfile.");
                    await publishingTask.FailAsync($"Building image for {resourceName} failed", cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }
        else
        {
            // Handle case when publishingTask is null (no step provided)
            try
            {
                await ContainerRuntime.BuildImageAsync(
                    contextPath,
                    dockerfilePath,
                    imageName,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build container image from Dockerfile.");
                throw;
            }
        }
    }

    private static async Task<IPublishingTask?> CreateTaskAsync(
        IPublishingStep? step,
        string description,
        CancellationToken cancellationToken)
    {

        if (step is null)
        {
            return null;
        }

        return await step.CreateTaskAsync(description, cancellationToken).ConfigureAwait(false);
    }

}
