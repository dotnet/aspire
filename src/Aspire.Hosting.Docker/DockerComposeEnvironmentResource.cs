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
    /// Determines whether to build container images for the resources in this environment.
    /// </summary>
    public bool BuildContainerImages { get; set; } = true;

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

            // Images built for Docker Compose should target the platform
            // the AppHost is running on so we allocate a build-step specifically
            // with the default container options
            foreach (var computeResource in model.GetComputeResources())
            {
                if (factoryContext.Resource.IsExcludedFromPublish())
                {
                    return [];
                }

                var buildStep = new PipelineStep
                {
                    Name = $"build-{computeResource.Name}-compose-{Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await containerImageBuilder.BuildImageAsync(
                            computeResource,
                            cancellationToken: ctx.CancellationToken).ConfigureAwait(false);
                    },
                    Resource = computeResource,
                    Tags = [WellKnownPipelineTags.BuildCompute],
                    RequiredBySteps = [WellKnownPipelineSteps.Build, WellKnownPipelineSteps.Deploy],
                    DependsOnSteps = [WellKnownPipelineSteps.BuildPrereq, WellKnownPipelineSteps.DeployPrereq]
                };
                buildStep.RequiredBy(dockerComposeUpStep);

                steps.Add(buildStep);
            }

            return steps;
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"compose -f \"{dockerComposeFilePath}\" up -d",
                        WorkingDirectory = outputPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(context.CancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    var stderr = await process.StandardError.ReadToEndAsync(context.CancellationToken).ConfigureAwait(false);
                    await deployTask.FailAsync($"docker compose up failed with exit code {process.ExitCode}: {stderr}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
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

    internal string AddEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        CapturedEnvironmentVariables[name] = (description, defaultValue, source);

        return $"${{{name}}}";
    }
}
