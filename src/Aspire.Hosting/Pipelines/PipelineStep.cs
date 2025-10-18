// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents a step in the deployment pipeline.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelineStep
{
    /// <summary>
    /// Gets or initializes the unique name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the action to execute for this step.
    /// </summary>
    public required Func<PipelineStepContext, Task> Action { get; init; }

    /// <summary>
    /// Gets the list of step names that this step depends on.
    /// </summary>
    public List<string> DependsOnSteps { get; } = [];

    /// <summary>
    /// Gets the list of step names that require this step to complete before they can finish.
    /// </summary>
    public List<string> RequiredBySteps { get; } = [];

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="stepName">The name of the step to depend on.</param>
    public void DependsOn(string stepName)
    {
        DependsOnSteps.Add(stepName);
    }

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="step">The step to depend on.</param>
    public void DependsOn(PipelineStep step)
    {
        DependsOnSteps.Add(step.Name);
    }

    /// <summary>
    /// Specifies that this step is required by another step.
    /// </summary>
    /// <param name="stepName">The name of the step that requires this step.</param>
    public void RequiredBy(string stepName)
    {
        RequiredBySteps.Add(stepName);
    }

    /// <summary>
    /// Specifies that this step is required by another step.
    /// </summary>
    /// <param name="step">The step that requires this step.</param>
    public void RequiredBy(PipelineStep step)
    {
        RequiredBySteps.Add(step.Name);
    }
}
