#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

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
                Action = async ctx =>
                {
                    var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                    await containerImageBuilder.BuildImageAsync(
                        this,
                        new ContainerBuildOptions
                        {
                            TargetPlatform = ContainerTargetPlatform.LinuxAmd64
                        },
                        ctx.CancellationToken).ConfigureAwait(false);
                },
                Tags = [WellKnownPipelineTags.BuildCompute],
                RequiredBySteps = [WellKnownPipelineSteps.Build],
                DependsOnSteps = [WellKnownPipelineSteps.BuildPrereq]
            };

            return [buildStep];
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
}
