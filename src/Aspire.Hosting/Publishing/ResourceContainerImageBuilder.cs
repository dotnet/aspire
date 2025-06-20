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
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IResourceContainerImageBuilder
{
    /// <summary>
    /// Builds a container that represents the specified resource.
    /// </summary>
    /// <param name="resource">The resource to build.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BuildImageAsync(IResource resource, CancellationToken cancellationToken);
}

internal sealed class ResourceContainerImageBuilder(
    ILogger<ResourceContainerImageBuilder> logger,
    IOptions<DcpOptions> dcpOptions,
    IServiceProvider serviceProvider,
    IPublishingActivityProgressReporter activityReporter) : IResourceContainerImageBuilder
{
    private const string ImageBuildStepId = "image-build";

    public async Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building container image for resource {Resource}", resource.Name);

        await activityReporter.CreateStepAsync(
            ImageBuildStepId,
            $"Building container image for resource {resource.Name}",
            cancellationToken
            ).ConfigureAwait(false);

        if (resource is ProjectResource)
        {
            // If it is a project resource we need to build the container image
            // using the .NET SDK.
            await BuildProjectContainerImageAsync(
                resource,
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
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            else
            {
                // Nothing to do here, the resource is already a container image.
                return;
            }
        }
        else
        {
            throw new NotSupportedException($"The resource type '{resource.GetType().Name}' is not supported.");
        }
    }

    private async Task BuildProjectContainerImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        var publishingTask = await activityReporter.CreateTaskAsync(
            $"{resource.Name}-build-image",
            ImageBuildStepId,
            $"Building image: {resource.Name}",
            cancellationToken
            ).ConfigureAwait(false);

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
                await activityReporter.CompleteTaskAsync(
                    publishingTask,
                    TaskCompletionState.CompletedWithError,
                    $"Building image: {resource.Name} failed",
                    cancellationToken).ConfigureAwait(false);
                throw new DistributedApplicationException($"Failed to build container image.");
            }
            else
            {
                await activityReporter.CompleteTaskAsync(
                    publishingTask,
                    TaskCompletionState.Completed,
                    $"Building image: {resource.Name} completed",
                    cancellationToken).ConfigureAwait(false);

                logger.LogDebug(
                    ".NET CLI completed with exit code: {ExitCode}",
                    processResult.ExitCode);
            }
        }
    }

    private async Task BuildContainerImageFromDockerfileAsync(string resourceName, string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        var publishingTask = await activityReporter.CreateTaskAsync(
            $"{resourceName}-build-image",
            ImageBuildStepId,
            $"Building image: {resourceName}",
            cancellationToken
            ).ConfigureAwait(false);

        try
        {
            var containerRuntime = dcpOptions.Value.ContainerRuntime switch
            {
                string rt => serviceProvider.GetRequiredKeyedService<IContainerRuntime>(rt),
                null => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("docker")
            };

            await containerRuntime.BuildImageAsync(
                contextPath,
                dockerfilePath,
                imageName,
                cancellationToken).ConfigureAwait(false);

            await activityReporter.CompleteTaskAsync(
                publishingTask,
                TaskCompletionState.Completed,
                $"Building image: {resourceName} completed",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build container image from Dockerfile.");

            await activityReporter.CompleteTaskAsync(
                publishingTask,
                TaskCompletionState.CompletedWithError,
                $"Building image: {resourceName} failed",
                cancellationToken).ConfigureAwait(false);

            throw;
        }

    }
}
