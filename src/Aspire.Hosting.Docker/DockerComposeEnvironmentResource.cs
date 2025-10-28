// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

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

    internal PortAllocator PortAllocator { get; } = new();

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

            var dockerComposeUpStep = new PipelineStep
            {
                Name = $"docker-compose-up-{Name}",
                Action = ctx => DockerComposeUpAsync(ctx),
                Tags = ["docker-compose-up"],
                DependsOnSteps = [$"publish-{Name}"]
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

            // Expand deployment target steps for all compute resources
            foreach (var computeResource in model.GetComputeResources())
            {
                if (factoryContext.Resource.IsExcludedFromPublish())
                {
                    return [];
                }

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
                context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute)
                        .RequiredBy(WellKnownPipelineSteps.Deploy)
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
        var activityReporter = context.PipelineContext.Services.GetRequiredService<IPipelineActivityReporter>();

        var dockerComposePublishingContext = new DockerComposePublishingContext(
            context.ExecutionContext,
            outputPath,
            context.Logger,
            activityReporter,
            context.CancellationToken);

        return dockerComposePublishingContext.WriteModelAsync(context.Model, this);
    }

    private async Task DockerComposeUpAsync(PipelineStepContext context)
    {
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(context, this);
        var dockerComposeFilePath = Path.Combine(outputPath, "docker-compose.yaml");

        if (!File.Exists(dockerComposeFilePath))
        {
            throw new InvalidOperationException($"Docker Compose file not found at {dockerComposeFilePath}");
        }

        var deployTask = await context.ReportingStep.CreateTaskAsync($"Running docker compose up for **{Name}**", context.CancellationToken).ConfigureAwait(false);
        await using (deployTask.ConfigureAwait(false))
        {
            try
            {
                var processSpec = new ProcessExecutionSpec(
                    FileName: "docker",
                    Arguments: $"compose -f \"{dockerComposeFilePath}\" up -d --remove-orphans",
                    WorkingDirectory: outputPath);

                var result = await ExecuteProcessAsync(processSpec, context.CancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    await deployTask.FailAsync($"docker compose up failed with exit code {result.ExitCode}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await deployTask.CompleteAsync($"Docker Compose deployment complete for **{Name}**", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
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

        if (!File.Exists(dockerComposeFilePath))
        {
            throw new InvalidOperationException($"Docker Compose file not found at {dockerComposeFilePath}");
        }

        var deployTask = await context.ReportingStep.CreateTaskAsync($"Running docker compose down for **{Name}**", context.CancellationToken).ConfigureAwait(false);
        await using (deployTask.ConfigureAwait(false))
        {
            try
            {
                var processSpec = new ProcessExecutionSpec(
                    FileName: "docker",
                    Arguments: $"compose -f \"{dockerComposeFilePath}\" down",
                    WorkingDirectory: outputPath);

                var result = await ExecuteProcessAsync(processSpec, context.CancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    await deployTask.FailAsync($"docker compose down failed with exit code {result.ExitCode}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await deployTask.CompleteAsync($"Docker Compose shutdown complete for **{Name}**", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await deployTask.CompleteAsync($"Docker Compose shutdown failed: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    internal string AddEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        CapturedEnvironmentVariables[name] = (description, defaultValue, source);

        return $"${{{name}}}";
    }

    private sealed record ProcessExecutionSpec(
        string FileName,
        string? Arguments = null,
        string? WorkingDirectory = null,
        bool RedirectOutput = true,
        bool RedirectError = true);

    private sealed record ProcessResult(int ExitCode);

    private static async Task<ProcessResult> ExecuteProcessAsync(ProcessExecutionSpec spec, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = spec.FileName,
                Arguments = spec.Arguments ?? string.Empty,
                WorkingDirectory = spec.WorkingDirectory ?? string.Empty,
                RedirectStandardOutput = spec.RedirectOutput,
                RedirectStandardError = spec.RedirectError,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new ProcessResult(process.ExitCode);
    }
}
