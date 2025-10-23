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

    internal IReadOnlyDictionary<PipelineStep, IResource> StepToResourceMap { get; init; } = null!;

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
        return StepToResourceMap.Where(kvp => kvp.Value == resource).Select(kvp => kvp.Key);
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
