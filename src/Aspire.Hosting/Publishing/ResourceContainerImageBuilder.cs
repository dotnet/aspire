// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Provides a service to publishers for building containers that represent a resource.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
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
    public async Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building container image for resource {Resource}", resource.Name);

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

    private async Task<string> BuildProjectContainerImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        var publishingActivity = await activityReporter.CreateActivityAsync(
            $"{resource.Name}-build-image",
            $"Building image: {resource.Name}",
            isPrimary: false,
            cancellationToken
            ).ConfigureAwait(false);

        // This is a resource project so we'll use the .NET SDK to build the container image.
        if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
        {
            throw new DistributedApplicationException($"The resource '{projectMetadata}' does not have a project metadata annotation.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(projectMetadata.ProjectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("/t:PublishContainer");
        startInfo.ArgumentList.Add($"/p:ContainerRepository={resource.Name}");

        logger.LogInformation(
            "Starting .NET CLI with arguments: {Arguments}",
            string.Join(" ", startInfo.ArgumentList.ToArray())
            );

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new DistributedApplicationException("Failed to start .NET CLI.");
        }

        logger.LogInformation(
            "Started .NET CLI with PID: {PID}",
            process.Id
            );

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

            await activityReporter.UpdateActivityStatusAsync(
                publishingActivity, (status) => status with { IsError = true },
                cancellationToken).ConfigureAwait(false);

            throw new DistributedApplicationException($"Failed to build container image, stdout: {stdout}, stderr: {stderr}");
        }
        else
        {
            await activityReporter.UpdateActivityStatusAsync(
                publishingActivity, (status) => status with { IsComplete = true },
                cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                ".NET CLI completed with exit code: {ExitCode}",
                process.ExitCode);

            return $"{resource.Name}:latest";
        }
    }

    private async Task<string> BuildContainerImageFromDockerfileAsync(string resourceName, string contextPath, string dockerfilePath, string imageName, CancellationToken cancellationToken)
    {
        var publishingActivity = await activityReporter.CreateActivityAsync(
            $"{resourceName}-build-image",
            $"Building image: {resourceName}",
            isPrimary: false,
            cancellationToken
            ).ConfigureAwait(false);

        try
        {
            var containerRuntime = dcpOptions.Value.ContainerRuntime switch
            {
                string rt => serviceProvider.GetRequiredKeyedService<IContainerRuntime>(rt),
                null => serviceProvider.GetRequiredKeyedService<IContainerRuntime>("docker")
            };

            var image = await containerRuntime.BuildImageAsync(
                contextPath,
                dockerfilePath,
                imageName,
                cancellationToken).ConfigureAwait(false);

            await activityReporter.UpdateActivityStatusAsync(
                publishingActivity, (status) => status with { IsComplete = true },
                cancellationToken).ConfigureAwait(false);

            return image;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build container image from Dockerfile.");

            await activityReporter.UpdateActivityStatusAsync(
                publishingActivity, (status) => status with { IsError = true },
                cancellationToken).ConfigureAwait(false);

            throw;
        }

    }
}