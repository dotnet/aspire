// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Provides contextual information for pipeline configuration callbacks.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineConfigurationContext
{
    /// <summary>
    /// Gets the service provider for dependency resolution.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the list of pipeline steps collected during the first pass.
    /// </summary>
    public required IReadOnlyList<PipelineStep> Steps { get; init; }

    /// <summary>
    /// Gets the distributed application model containing all resources.
    /// </summary>
    public required DistributedApplicationModel Model { get; init; }

    internal IReadOnlyDictionary<PipelineStep, IResource>? StepToResourceMap { get; init; }

    /// <summary>
    /// Gets all pipeline steps with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to search for.</param>
    /// <returns>A collection of steps that have the specified tag.</returns>
    public IEnumerable<PipelineStep> GetSteps(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return Steps.Where(s => s.Tags.Contains(tag));
    }

    /// <summary>
    /// Gets all pipeline steps associated with the specified resource.
    /// </summary>
    /// <param name="resource">The resource to search for.</param>
    /// <returns>A collection of steps associated with the resource.</returns>
    public IEnumerable<PipelineStep> GetSteps(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return StepToResourceMap?.Where(kvp => kvp.Value == resource).Select(kvp => kvp.Key) ?? [];
    }

    /// <summary>
    /// Gets all pipeline steps with the specified tag that are associated with the specified resource.
    /// </summary>
    /// <param name="resource">The resource to search for.</param>
    /// <param name="tag">The tag to search for.</param>
    /// <returns>A collection of steps that have the specified tag and are associated with the resource.</returns>
    public IEnumerable<PipelineStep> GetSteps(IResource resource, string tag)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(tag);
        return GetSteps(resource).Where(s => s.Tags.Contains(tag));
    }
}

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// Extension methods for pipeline steps.
/// </summary>
public static class PipelineStepsExtensions
{
    /// <summary>
    /// Makes each step in the collection depend on the specified step.
    /// </summary>
    /// <param name="steps">The collection of steps.</param>
    /// <param name="step">The step to depend on.</param>
    /// <returns>The original collection of steps.</returns>
    public static IEnumerable<PipelineStep> DependsOn(this IEnumerable<PipelineStep> steps, PipelineStep? step)
    {
        if (step is null)
        {
            return steps;
        }

        foreach (var s in steps)
        {
            s.DependsOn(step);
        }

        return steps;
    }

    /// <summary>
    /// Makes each step in the collection depend on the specified target steps.
    /// </summary>
    /// <param name="steps">The collection of steps.</param>
    /// <param name="targetSteps">The target steps to depend on.</param>
    /// <returns>The original collection of steps.</returns>
    public static IEnumerable<PipelineStep> DependsOn(this IEnumerable<PipelineStep> steps, IEnumerable<PipelineStep> targetSteps)
    {
        foreach (var step in targetSteps)
        {
            foreach (var s in steps)
            {
                s.DependsOn(step);
            }
        }

        return steps;
    }

    /// <summary>
    /// Specifies that each step in the collection is required by the specified step.
    /// </summary>
    /// <param name="steps">The collection of steps.</param>
    /// <param name="step">The step that requires these steps.</param>
    /// <returns>The original collection of steps.</returns>
    public static IEnumerable<PipelineStep> RequiredBy(this IEnumerable<PipelineStep> steps, PipelineStep? step)
    {
        if (step is null)
        {
            return steps;
        }

        foreach (var s in steps)
        {
            s.RequiredBy(step);
        }

        return steps;
    }

    /// <summary>
    /// Specifies that each step in the collection is required by the specified target steps.
    /// </summary>
    /// <param name="steps">The collection of steps.</param>
    /// <param name="targetSteps">The target steps that require these steps.</param>
    /// <returns>The original collection of steps.</returns>
    public static IEnumerable<PipelineStep> RequiredBy(this IEnumerable<PipelineStep> steps, IEnumerable<PipelineStep> targetSteps)
    {
        foreach (var step in targetSteps)
        {
            foreach (var s in steps)
            {
                s.RequiredBy(step);
            }
        }

        return steps;
    }
}