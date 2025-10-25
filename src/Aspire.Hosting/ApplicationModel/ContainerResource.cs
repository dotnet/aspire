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
/// A resource that represents a specified container.
/// </summary>
public class ContainerResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport, IResourceWithProbes,
    IComputeResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="entrypoint">An optional container entrypoint.</param>
    public ContainerResource(string name, string? entrypoint = null) : base(name)
    {
        Entrypoint = entrypoint;

        // Add pipeline step annotation to create a build step for this container
        // Only create if there's a DockerfileBuildAnnotation
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            // Only generate the build step if there's a DockerfileBuildAnnotation
            if (!this.TryGetLastAnnotation<DockerfileBuildAnnotation>(out _))
            {
                return await Task.FromResult<IEnumerable<PipelineStep>>([]).ConfigureAwait(false);
            }

            var buildStep = new PipelineStep
            {
                Name = $"build-{name}",
                Action = async ctx =>
                {
                    var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                    await containerImageBuilder.BuildImagesAsync(
                        [this],
                        new ContainerBuildOptions
                        {
                            TargetPlatform = ContainerTargetPlatform.LinuxAmd64
                        },
                        ctx.CancellationToken).ConfigureAwait(false);
                },
                Tags = [WellKnownPipelineTags.BuildCompute, "build"]
            };

            return await Task.FromResult<IEnumerable<PipelineStep>>([buildStep]).ConfigureAwait(false);
        }));
    }

    /// <summary>
    /// The container Entrypoint.
    /// </summary>
    /// <remarks><c>null</c> means use the default Entrypoint defined by the container.</remarks>
    public string? Entrypoint { get; set; }
}
