// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents a step in the deployment pipeline.
/// </summary>
public class PipelineStep
{
    /// <summary>
    /// Gets or initializes the unique name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the action to execute for this step.
    /// </summary>
    public required Func<DeployingContext, Task> Action { get; init; }

    /// <summary>
    /// Gets the list of step names that this step depends on.
    /// </summary>
    public List<string> Dependencies { get; } = [];

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="stepName">The name of the step to depend on.</param>
    public void DependsOnStep(string stepName)
    {
        Dependencies.Add(stepName);
    }

    /// <summary>
    /// Adds a dependency on another step.
    /// </summary>
    /// <param name="step">The step to depend on.</param>
    public void DependsOnStep(PipelineStep step)
    {
        Dependencies.Add(step.Name);
    }
}
