// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a Docker Compose environment resource that can host application resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DockerComposeEnvironmentResource"/> class.
/// </remarks>
public class DockerComposeEnvironmentResource : Resource, IComputeEnvironmentResource
{
    /// <summary>
    /// The container registry to use.
    /// </summary>
    public string? DefaultContainerRegistry { get; set; }

    /// <summary>
    /// The name of an existing network to be used.
    /// </summary>
    public string? DefaultNetworkName { get; set; }

    /// <summary>
    /// Determines whether to include an Aspire dashboard for telemetry visualization in this environment.
    /// </summary>
    public bool DashboardEnabled { get; set; } = true;

    internal Action<ComposeFile>? ConfigureComposeFile { get; set; }

    internal IResourceBuilder<DockerComposeAspireDashboardResource>? Dashboard { get; set; }

    /// <summary>
    /// Gets the collection of environment variables captured from the Docker Compose environment.
    /// These will be populated into a top-level .env file adjacent to the Docker Compose file.
    /// </summary>
    internal Dictionary<string, (string? Description, string? DefaultValue, object? Source)> CapturedEnvironmentVariables { get; } = [];

    internal Dictionary<IResource, DockerComposeServiceResource> ResourceMapping { get; } = new(new ResourceNameComparer());

    internal IPortAllocator PortAllocator { get; } = new PortAllocator();

    /// <param name="name">The name of the Docker Compose environment.</param>
    public DockerComposeEnvironmentResource(string name) : base(name)
    {
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            var publishStep = new PipelineStep
            {
                Name = $"publish-{Name}",
                Action = ctx => PublishAsync(ctx)
            };
            publishStep.RequiredBy(WellKnownPipelineSteps.Publish);
            steps.Add(publishStep);

            // Expand deployment target steps for all compute resources
            foreach (var computeResource in model.GetComputeResources())
            {
                var deploymentTarget = computeResource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;

                if (deploymentTarget != null && deploymentTarget.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var annotations))
                {
                    // Resolve the deployment target's PipelineStepAnnotation and expand its steps
                    // We do this because the deployment target is not in the model
                    foreach (var annotation in annotations)
                    {
                        var childFactoryContext = new PipelineStepFactoryContext
                        {
                            PipelineContext = factoryContext.PipelineContext,
                            Resource = deploymentTarget
                        };

                        var deploymentTargetSteps = await annotation.CreateStepsAsync(childFactoryContext).ConfigureAwait(false);

                        foreach (var step in deploymentTargetSteps)
                        {
                            // Ensure the step is associated with the deployment target resource
                            step.Resource ??= deploymentTarget;
                        }

                        steps.AddRange(deploymentTargetSteps);
                    }
                }
            }

            var prepareStep = new PipelineStep
            {
                Name = $"prepare-{Name}",
                Action = ctx => PrepareAsync(ctx)
            };
            prepareStep.DependsOn(WellKnownPipelineSteps.Publish);
            prepareStep.DependsOn(WellKnownPipelineSteps.Build);
            steps.Add(prepareStep);

            var dockerComposeUpStep = new PipelineStep
            {
                Name = $"docker-compose-up-{Name}",
                Action = ctx => DockerComposeUpAsync(ctx),
                Tags = ["docker-compose-up"],
                DependsOnSteps = [$"prepare-{Name}"]
            };
            dockerComposeUpStep.RequiredBy(WellKnownPipelineSteps.Deploy);
            steps.Add(dockerComposeUpStep);

            var dockerComposeDownStep = new PipelineStep
            {
                Name = $"docker-compose-down-{Name}",
                Action = ctx => DockerComposeDownAsync(ctx),
                Tags = ["docker-compose-down"]
            };
            steps.Add(dockerComposeDownStep);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        // This is where we wire up the build steps created by the resources
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            // Wire up build step dependencies
            // Build steps are created by ProjectResource and ContainerResource
            foreach (var computeResource in context.Model.GetComputeResources())
            {
                var deploymentTarget = computeResource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;

                if (deploymentTarget is null)
                {
                    continue;
                }

                // Execute the PipelineConfigurationAnnotation callbacks on the deployment target
                if (deploymentTarget.TryGetAnnotationsOfType<PipelineConfigurationAnnotation>(out var annotations))
                {
                    foreach (var annotation in annotations)
                    {
                        annotation.Callback(context);
                    }
                }
            }

            // This ensures that resources that have to be built before deployments are handled
            foreach (var computeResource in context.Model.GetBuildResources())
            {
                var buildSteps = context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute);

                buildSteps.RequiredBy(WellKnownPipelineSteps.Deploy)
                        .RequiredBy($"docker-compose-up-{Name}")
                        .DependsOn(WellKnownPipelineSteps.DeployPrereq);
            }
        }));
    }

    /// <summary>
    /// Computes the host URL <see cref="ReferenceExpression"/> for the given <see cref="EndpointReference"/>.
    /// </summary>
    /// <param name="endpointReference">The endpoint reference to compute the host address for.</param>
    /// <returns>A <see cref="ReferenceExpression"/> representing the host address.</returns>
    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
    {
        var resource = endpointReference.Resource;

        // In Docker Compose, services can communicate using their service names
        // Docker Compose automatically creates a network where services can reach each other by service name
        return ReferenceExpression.Create($"{resource.Name.ToLowerInvariant()}");
    }

    private Task PublishAsync(PipelineStepContext context)
    {
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(context, this);
        var imageBuilder = context.Services.GetRequiredService<IResourceContainerImageManager>();

        var dockerComposePublishingContext = new DockerComposePublishingContext(
            context.ExecutionContext,
            imageBuilder,
            outputPath,
            context.Logger,
            context.ReportingStep,
            context.CancellationToken);

        return dockerComposePublishingContext.WriteModelAsync(context.Model, this);
    }

    private async Task DockerComposeUpAsync(PipelineStepContext context)
    {
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(context, this);
        var dockerComposeFilePath = Path.Combine(outputPath, "docker-compose.yaml");
        var envFilePath = GetEnvFilePath(context);

        if (!File.Exists(dockerComposeFilePath))
        {
            throw new InvalidOperationException($"Docker Compose file not found at {dockerComposeFilePath}");
        }

        var deployTask = await context.ReportingStep.CreateTaskAsync($"Running docker compose up for **{Name}**", context.CancellationToken).ConfigureAwait(false);
        await using (deployTask.ConfigureAwait(false))
        {
            try
            {
                var projectName = GetDockerComposeProjectName(context);
                var arguments = $"compose -f \"{dockerComposeFilePath}\" --project-name \"{projectName}\"";

                if (File.Exists(envFilePath))
                {
                    arguments += $" --env-file \"{envFilePath}\"";
                }

                arguments += " up -d --remove-orphans";

                context.Logger.LogDebug("Running docker compose up with project name: {ProjectName}, arguments: {Arguments}", projectName, arguments);

                var spec = new ProcessSpec("docker")
                {
                    Arguments = arguments,
                    WorkingDirectory = outputPath,
                    ThrowOnNonZeroReturnCode = false,
                    InheritEnv = true,
                    OnOutputData = output =>
                    {
                        context.Logger.LogDebug("docker compose up (stdout): {Output}", output);
                    },
                    OnErrorData = error =>
                    {
                        context.Logger.LogDebug("docker compose up (stderr): {Error}", error);
                    },
                };

                var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

                await using (processDisposable)
                {
                    var processResult = await pendingProcessResult
                        .WaitAsync(context.CancellationToken)
                        .ConfigureAwait(false);

                    if (processResult.ExitCode != 0)
                    {
                        await deployTask.FailAsync($"docker compose up failed with exit code {processResult.ExitCode}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await deployTask.CompleteAsync($"Service **{Name}** is now running with Docker Compose locally", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await deployTask.CompleteAsync($"Docker Compose deployment failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task DockerComposeDownAsync(PipelineStepContext context)
    {
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(context, this);
        var dockerComposeFilePath = Path.Combine(outputPath, "docker-compose.yaml");
        var envFilePath = GetEnvFilePath(context);

        if (!File.Exists(dockerComposeFilePath))
        {
            throw new InvalidOperationException($"Docker Compose file not found at {dockerComposeFilePath}");
        }

        var deployTask = await context.ReportingStep.CreateTaskAsync($"Running docker compose down for **{Name}**", context.CancellationToken).ConfigureAwait(false);
        await using (deployTask.ConfigureAwait(false))
        {
            try
            {
                var projectName = GetDockerComposeProjectName(context);
                var arguments = $"compose -f \"{dockerComposeFilePath}\" --project-name \"{projectName}\"";

                if (File.Exists(envFilePath))
                {
                    arguments += $" --env-file \"{envFilePath}\"";
                }

                arguments += " down";

                context.Logger.LogDebug("Running docker compose down with project name: {ProjectName}, arguments: {Arguments}", projectName, arguments);

                var spec = new ProcessSpec("docker")
                {
                    Arguments = arguments,
                    WorkingDirectory = outputPath,
                    ThrowOnNonZeroReturnCode = false,
                    InheritEnv = true
                };

                var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

                await using (processDisposable)
                {
                    var processResult = await pendingProcessResult
                        .WaitAsync(context.CancellationToken)
                        .ConfigureAwait(false);

                    if (processResult.ExitCode != 0)
                    {
                        await deployTask.FailAsync($"docker compose down failed with exit code {processResult.ExitCode}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await deployTask.CompleteAsync($"Docker Compose shutdown complete for **{Name}**", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await deployTask.CompleteAsync($"Docker Compose shutdown failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task PrepareAsync(PipelineStepContext context)
    {
        var envFilePath = GetEnvFilePath(context);

        if (CapturedEnvironmentVariables.Count == 0)
        {
            return;
        }

        // Initialize a new EnvFile for this environment
        var envFile = EnvFile.Create(envFilePath, context.Logger);

        foreach (var entry in CapturedEnvironmentVariables)
        {
            var (key, (description, defaultValue, source)) = entry;

            if (defaultValue is null && source is ParameterResource parameter)
            {
                defaultValue = await parameter.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
            }

            if (source is ContainerImageReference cir && cir.Resource.TryGetContainerImageName(out var imageName))
            {
                defaultValue = imageName;
            }

            envFile.Add(key, defaultValue, description, onlyIfMissing: false);
        }

        envFile.Save(includeValues: true);
    }

    internal string AddEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        CapturedEnvironmentVariables[name] = (description, defaultValue, source);

        return $"${{{name}}}";
    }

    private string GetDockerComposeProjectName(PipelineStepContext context)
    {
        // Get the AppHost:PathSha256 from configuration to disambiguate projects
        var configuration = context.Services.GetService<IConfiguration>();
        var appHostSha = configuration?["AppHost:PathSha256"];

        if (!string.IsNullOrEmpty(appHostSha) && appHostSha.Length >= 8)
        {
            // Use first 8 characters of the hash for readability
            // Format: aspire-{environmentName}-{sha8}
            return $"aspire-{Name.ToLowerInvariant()}-{appHostSha[..8].ToLowerInvariant()}";
        }

        // Fallback to just using the environment name if PathSha256 is not available
        return $"aspire-{Name.ToLowerInvariant()}";
    }

    private string GetEnvFilePath(PipelineStepContext context)
    {
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(context, this);
        var hostEnvironment = context.Services.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
        var environmentName = hostEnvironment?.EnvironmentName ?? Name;
        var envFilePath = Path.Combine(outputPath, $".env.{environmentName}");
        return envFilePath;
    }
}
