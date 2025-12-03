#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a general-purpose container registry resource that can be used to reference external container registries
/// (e.g., Docker Hub, GitHub Container Registry, or private registries) in the application model.
/// </summary>
/// <remarks>
/// This resource implements <see cref="IContainerRegistry"/> and allows configuration using either
/// <see cref="ParameterResource"/> values or hard-coded strings, providing flexibility for scenarios
/// where registry configuration needs to be dynamically provided or statically defined.
/// Use <see cref="ContainerRegistryResourceBuilderExtensions.AddContainerRegistry(IDistributedApplicationBuilder, string, IResourceBuilder{ParameterResource}, IResourceBuilder{ParameterResource}?)"/>
/// to add a container registry with parameterized values, or
/// <see cref="ContainerRegistryResourceBuilderExtensions.AddContainerRegistry(IDistributedApplicationBuilder, string, string, string?)"/>
/// to add a container registry with literal values.
/// </remarks>
/// <example>
/// Add a container registry with parameterized values:
/// <code>
/// var endpointParameter = builder.AddParameter("registry-endpoint");
/// var repositoryParameter = builder.AddParameter("registry-repo");
/// var registry = builder.AddContainerRegistry("my-registry", endpointParameter, repositoryParameter);
/// </code>
/// </example>
/// <example>
/// Add a container registry with literal values:
/// <code>
/// var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "myusername");
/// </code>
/// </example>
[Experimental("ASPIRECOMPUTE003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ContainerRegistryResource : Resource, IContainerRegistry
{
    private readonly ReferenceExpression _registryName;
    private readonly ReferenceExpression _endpoint;
    private readonly ReferenceExpression? _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerRegistryResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="endpoint">The endpoint URL or hostname of the container registry.</param>
    /// <param name="repository">The optional repository path within the container registry.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoint"/> is <see langword="null"/>.</exception>
    public ContainerRegistryResource(string name, ReferenceExpression endpoint, ReferenceExpression? repository = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        _registryName = ReferenceExpression.Create($"{name}");
        _endpoint = endpoint;
        _repository = repository;

        // Add pipeline step annotation to create push steps for resources that require image build and push
        Annotations.Add(new PipelineStepAnnotation(factoryContext =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            foreach (var resource in GetResourcesToPush(model))
            {
                var pushStep = new PipelineStep
                {
                    Name = $"push-{resource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageManager = ctx.Services.GetRequiredService<IResourceContainerImageManager>();
                        await containerImageManager.PushImageAsync(resource, ctx.CancellationToken).ConfigureAwait(false);
                    },
                    Tags = [WellKnownPipelineTags.PushContainerImage],
                    RequiredBySteps = [WellKnownPipelineSteps.Push],
                    Resource = resource
                };

                steps.Add(pushStep);
            }

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies between build and push steps
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            foreach (var resource in GetResourcesToPush(context.Model))
            {
                var buildSteps = context.GetSteps(resource, WellKnownPipelineTags.BuildCompute);
                var resourcePushSteps = context.GetSteps(resource, WellKnownPipelineTags.PushContainerImage);

                foreach (var pushStep in resourcePushSteps)
                {
                    foreach (var buildStep in buildSteps)
                    {
                        pushStep.DependsOn(buildStep);
                    }
                    pushStep.DependsOn(WellKnownPipelineSteps.PushPrereq);
                }
            }
        }));
    }

    private static IEnumerable<IResource> GetResourcesToPush(DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (!resource.RequiresImageBuildAndPush())
            {
                continue;
            }

            yield return resource;
        }
    }

    /// <inheritdoc />
    ReferenceExpression IContainerRegistry.Name => _registryName;

    /// <inheritdoc />
    ReferenceExpression IContainerRegistry.Endpoint => _endpoint;

    /// <inheritdoc />
    ReferenceExpression? IContainerRegistry.Repository => _repository;
}
