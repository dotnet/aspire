#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel.Docker;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
public class ProjectResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery, IResourceWithWaitSupport, IResourceWithProbes,
    IComputeResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public ProjectResource(string name) : base(name)
    {
        // Add pipeline step annotation to create a build step for this project
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            if (factoryContext.Resource.IsExcludedFromPublish())
            {
                return [];
            }

            var buildStep = new PipelineStep
            {
                Name = $"build-{name}",
                Action = BuildProjectImage,
                Tags = [WellKnownPipelineTags.BuildCompute],
                RequiredBySteps = [WellKnownPipelineSteps.Build],
                DependsOnSteps = [WellKnownPipelineSteps.BuildPrereq]
            };

            return [buildStep];
        }));

        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            // Ensure any static file references' images are built first
            if (this.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesAnnotations))
            {
                var buildSteps = context.GetSteps(this, WellKnownPipelineTags.BuildCompute);

                foreach (var containerFile in containerFilesAnnotations)
                {
                    buildSteps.DependsOn(context.GetSteps(containerFile.Source, WellKnownPipelineTags.BuildCompute));
                }
            }
        }));
    }
    // Keep track of the config host for each Kestrel endpoint annotation
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    internal bool ShouldInjectEndpointEnvironment(EndpointReference e)
    {
        var endpoint = e.EndpointAnnotation;

        if (endpoint.UriScheme is not ("http" or "https") ||    // Only process http and https endpoints
            endpoint.TargetPortEnvironmentVariable is not null) // Skip if target port env variable was set
        {
            return false;
        }

        // If any filter rejects the endpoint, skip it
        return !Annotations.OfType<EndpointEnvironmentInjectionFilterAnnotation>()
            .Select(a => a.Filter)
            .Any(f => !f(endpoint));
    }

    private async Task BuildProjectImage(PipelineStepContext ctx)
    {
        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
        var logger = ctx.Logger;

        // Build the container image for the project first
        await containerImageBuilder.BuildImageAsync(
            this,
            new ContainerBuildOptions
            {
                TargetPlatform = ContainerTargetPlatform.AllLinux
            },
            ctx.CancellationToken).ConfigureAwait(false);

        // Check if we need to copy container files
        if (!this.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesAnnotations))
        {
            // No container files to copy, just build the image normally
            return;
        }

        // Get the built image name
        var originalImageName = Name.ToLowerInvariant();

        // Tag the built image with a temporary tag
        var tempTag = $"temp-{Guid.NewGuid():N}";
        var tempImageName = $"{originalImageName}:{tempTag}";

        var containerRuntime = ctx.Services.GetRequiredService<IContainerRuntime>();

        logger.LogDebug("Tagging image {OriginalImageName} as {TempImageName}", originalImageName, tempImageName);
        await containerRuntime.TagImageAsync(originalImageName, tempImageName, ctx.CancellationToken).ConfigureAwait(false);

        // Generate a Dockerfile that layers the container files on top
        var dockerfileBuilder = new DockerfileBuilder();
        var stage = dockerfileBuilder.From(tempImageName);

        var projectMetadata = this.GetProjectMetadata();

        // Get the container working directory for the project
        var containerWorkingDir = await GetContainerWorkingDirectoryAsync(projectMetadata.ProjectPath, logger, ctx.CancellationToken).ConfigureAwait(false);

        // Add COPY --from: statements for each source
        foreach (var containerFileDestination in containerFilesAnnotations)
        {
            var source = containerFileDestination.Source;

            if (!source.TryGetContainerImageName(out var sourceImageName))
            {
                logger.LogWarning("Cannot get container image name for source resource {SourceName}, skipping", source.Name);
                continue;
            }

            var destinationPath = containerFileDestination.DestinationPath;
            if (!destinationPath.StartsWith('/'))
            {
                // Make it an absolute path relative to the container working directory
                destinationPath = $"{containerWorkingDir}/{destinationPath}";
            }

            foreach (var containerFilesSource in source.Annotations.OfType<ContainerFilesSourceAnnotation>())
            {
                logger.LogDebug("Adding COPY --from={SourceImage} {SourcePath} {DestinationPath}",
                    sourceImageName, containerFilesSource.SourcePath, destinationPath);
                stage.CopyFrom(sourceImageName, containerFilesSource.SourcePath, destinationPath);
            }
        }

        // Write the Dockerfile to a temporary location
        var projectDir = Path.GetDirectoryName(projectMetadata.ProjectPath)!;
        var tempDockerfilePath = Path.GetTempFileName();

        var builtSuccessfully = false;
        try
        {
            using (var writer = new StreamWriter(tempDockerfilePath))
            {
                await dockerfileBuilder.WriteAsync(writer, ctx.CancellationToken).ConfigureAwait(false);
            }

            logger.LogDebug("Generated temporary Dockerfile at {DockerfilePath}", tempDockerfilePath);

            // Build the final image from the generated Dockerfile
            await containerRuntime.BuildImageAsync(
                projectDir,
                tempDockerfilePath,
                originalImageName,
                new ContainerBuildOptions
                {
                    TargetPlatform = ContainerTargetPlatform.AllLinux
                },
                [],
                [],
                null,
                ctx.CancellationToken).ConfigureAwait(false);

            logger.LogDebug("Successfully built final image {ImageName} with container files", originalImageName);
            builtSuccessfully = true;
        }
        finally
        {
            if (builtSuccessfully)
            {
                // Clean up the temporary Dockerfile
                if (File.Exists(tempDockerfilePath))
                {
                    try
                    {
                        File.Delete(tempDockerfilePath);
                        logger.LogDebug("Deleted temporary Dockerfile {DockerfilePath}", tempDockerfilePath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete temporary Dockerfile {DockerfilePath}", tempDockerfilePath);
                    }
                }
            }
            else
            {
                // Keep the Dockerfile for debugging purposes
                logger.LogDebug("Failed build - temporary Dockerfile left at {DockerfilePath} for debugging", tempDockerfilePath);
            }

            // Remove the temporary tagged image
            logger.LogDebug("Removing temporary image {TempImageName}", tempImageName);
            await containerRuntime.RemoveImageAsync(tempImageName, ctx.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string> GetContainerWorkingDirectoryAsync(string projectPath, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var outputLines = new List<string>();
            var spec = new Dcp.Process.ProcessSpec("dotnet")
            {
                Arguments = $"msbuild -getProperty:ContainerWorkingDirectory \"{projectPath}\"",
                OnOutputData = output =>
                {
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        outputLines.Add(output.Trim());
                    }
                },
                OnErrorData = error => logger.LogDebug("dotnet msbuild (stderr): {Error}", error),
                ThrowOnNonZeroReturnCode = false
            };

            logger.LogDebug("Getting ContainerWorkingDirectory for project {ProjectPath}", projectPath);
            var (pendingResult, processDisposable) = Dcp.Process.ProcessUtil.Run(spec);

            await using (processDisposable.ConfigureAwait(false))
            {
                var result = await pendingResult.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    logger.LogDebug("Failed to get ContainerWorkingDirectory from dotnet msbuild for project {ProjectPath}. Exit code: {ExitCode}. Using default /app",
                        projectPath, result.ExitCode);
                    return "/app";
                }

                // The last non-empty line should contain the ContainerWorkingDirectory value
                var workingDir = outputLines.LastOrDefault();

                if (string.IsNullOrWhiteSpace(workingDir))
                {
                    logger.LogDebug("dotnet msbuild returned empty ContainerWorkingDirectory for project {ProjectPath}. Using default /app", projectPath);
                    return "/app";
                }

                logger.LogDebug("Resolved ContainerWorkingDirectory for project {ProjectPath}: {WorkingDir}", projectPath, workingDir);
                return workingDir;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error getting ContainerWorkingDirectory. Using default /app");
            return "/app";
        }
    }
}
