// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a pipeline of deployment steps with dependency resolution.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class Pipeline
{
    private readonly List<PipelineStep> _steps = [];

    /// <summary>
    /// Adds a step to the pipeline.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(PipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
    }

    /// <summary>
    /// Executes all steps in the pipeline in dependency order.
    /// </summary>
    /// <param name="context">The deployment context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(DeployingContext context, CancellationToken cancellationToken = default)
    {
        var executedSteps = new HashSet<string>();
        var stepsByName = _steps.ToDictionary(s => s.Name, s => s);

        var sortedSteps = TopologicalSort(stepsByName);

        foreach (var step in sortedSteps)
        {
            if (step.Action is null)
            {
                continue;
            }

            await step.Action(context).ConfigureAwait(false);
            executedSteps.Add(step.Name);
        }
    }

    private static List<PipelineStep> TopologicalSort(Dictionary<string, PipelineStep> stepsByName)
    {
        var result = new List<PipelineStep>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(string stepName)
        {
            if (visited.Contains(stepName))
            {
                return;
            }

            if (visiting.Contains(stepName))
            {
                throw new InvalidOperationException($"Circular dependency detected involving step '{stepName}'");
            }

            if (!stepsByName.TryGetValue(stepName, out var step))
            {
                throw new InvalidOperationException($"Step '{stepName}' referenced as a dependency but not found in pipeline");
            }

            visiting.Add(stepName);

            foreach (var dependency in step.Dependencies)
            {
                Visit(dependency);
            }

            visiting.Remove(stepName);
            visited.Add(stepName);
            result.Add(step);
        }

        foreach (var stepName in stepsByName.Keys)
        {
            Visit(stepName);
        }

        return result;
    }
}
