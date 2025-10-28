// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
        Annotations.Add(new PipelineStepAnnotation(context =>
        {
            var step = new PipelineStep
            {
                Name = $"publish-{Name}",
                Action = ctx => PublishAsync(ctx)
            };
            step.RequiredBy(WellKnownPipelineSteps.Publish);
            return step;
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
        var imageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();

        var dockerComposePublishingContext = new DockerComposePublishingContext(
            context.ExecutionContext,
            imageBuilder,
            outputPath,
            context.Logger,
            activityReporter,
            context.CancellationToken);

        return dockerComposePublishingContext.WriteModelAsync(context.Model, this);
    }

    internal string AddEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        CapturedEnvironmentVariables[name] = (description, defaultValue, source);

        return $"${{{name}}}";
    }
}
