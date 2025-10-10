// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class DistributedApplicationPipeline : IDistributedApplicationPipeline
{
    private readonly List<PipelineStep> _steps = [];

    public void AddStep(string name,
        Func<DeployingContext, Task> action,
        object? dependsOn = null,
        object? requiredBy = null)
    {
        if (_steps.Any(s => s.Name == name))
        {
            throw new InvalidOperationException(
                $"A step with the name '{name}' has already been added to the pipeline.");
        }

        var step = new PipelineStep
        {
            Name = name,
            Action = action
        };

        if (dependsOn != null)
        {
            AddDependencies(step, dependsOn);
        }

        if (requiredBy != null)
        {
            AddRequiredBy(step, requiredBy);
        }

        _steps.Add(step);
    }

    private static void AddDependencies(PipelineStep step, object dependsOn)
    {
        if (dependsOn is string stepName)
        {
            step.DependsOn(stepName);
        }
        else if (dependsOn is IEnumerable<string> stepNames)
        {
            foreach (var name in stepNames)
            {
                step.DependsOn(name);
            }
        }
        else
        {
            throw new ArgumentException(
                $"The dependsOn parameter must be a string or IEnumerable<string>, but was {dependsOn.GetType().Name}.",
                nameof(dependsOn));
        }
    }

    private static void AddRequiredBy(PipelineStep step, object requiredBy)
    {
        if (requiredBy is string stepName)
        {
            step.IsRequiredBy(stepName);
        }
        else if (requiredBy is IEnumerable<string> stepNames)
        {
            foreach (var name in stepNames)
            {
                step.IsRequiredBy(name);
            }
        }
        else
        {
            throw new ArgumentException(
                $"The requiredBy parameter must be a string or IEnumerable<string>, but was {requiredBy.GetType().Name}.",
                nameof(requiredBy));
        }
    }

    public void AddStep(PipelineStep step)
    {
        if (_steps.Any(s => s.Name == step.Name))
        {
            throw new InvalidOperationException(
                $"A step with the name '{step.Name}' has already been added to the pipeline.");
        }

        _steps.Add(step);
    }

    public async Task ExecuteAsync(DeployingContext context)
    {
        var allSteps = _steps.Concat(CollectStepsFromAnnotations(context)).ToList();

        if (allSteps.Count == 0)
        {
            return;
        }

        ValidateSteps(allSteps);

        var stepsByName = allSteps.ToDictionary(s => s.Name);

        var levels = ResolveDependencies(allSteps, stepsByName);

        foreach (var level in levels)
        {
            await Task.WhenAll(level.Select(step =>
                ExecuteStepAsync(step, context))).ConfigureAwait(false);
        }
    }

    private static IEnumerable<PipelineStep> CollectStepsFromAnnotations(DeployingContext context)
    {
        foreach (var resource in context.Model.Resources)
        {
            var annotations = resource.Annotations
                .OfType<PipelineStepAnnotation>();

            foreach (var annotation in annotations)
            {
                foreach (var step in annotation.CreateSteps())
                {
                    yield return step;
                }
            }
        }
    }

    private static void ValidateSteps(IEnumerable<PipelineStep> steps)
    {
        var stepNames = new HashSet<string>();

        foreach (var step in steps)
        {
            if (!stepNames.Add(step.Name))
            {
                throw new InvalidOperationException(
                    $"Duplicate step name: '{step.Name}'");
            }
        }

        foreach (var step in steps)
        {
            foreach (var dependency in step.Dependencies)
            {
                if (!stepNames.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' depends on unknown step '{dependency}'");
                }
            }

            foreach (var requiredBy in step.RequiredBy)
            {
                if (!stepNames.Contains(requiredBy))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredBy}'");
                }
            }
        }
    }

    private static List<List<PipelineStep>> ResolveDependencies(
        IEnumerable<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName)
    {
        var graph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        foreach (var step in steps)
        {
            graph[step.Name] = [];
            inDegree[step.Name] = 0;
        }

        foreach (var step in steps)
        {
            foreach (var requiredByStep in step.RequiredBy)
            {
                if (!graph.ContainsKey(requiredByStep))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredByStep}'");
                }

                if (stepsByName.TryGetValue(requiredByStep, out var requiredByStepObj) &&
                    !requiredByStepObj.Dependencies.Contains(step.Name))
                {
                    requiredByStepObj.Dependencies.Add(step.Name);
                }
            }
        }

        foreach (var step in steps)
        {
            foreach (var dependency in step.Dependencies)
            {
                if (!graph.TryGetValue(dependency, out var dependents))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' depends on unknown step '{dependency}'");
                }

                dependents.Add(step.Name);
                inDegree[step.Name]++;
            }
        }

        var levels = new List<List<PipelineStep>>();
        var queue = new Queue<string>(
            inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key)
        );

        while (queue.Count > 0)
        {
            var currentLevel = new List<PipelineStep>();
            var levelSize = queue.Count;

            for (var i = 0; i < levelSize; i++)
            {
                var stepName = queue.Dequeue();
                var step = stepsByName[stepName];
                currentLevel.Add(step);

                foreach (var dependent in graph[stepName])
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            levels.Add(currentLevel);
        }

        if (levels.Sum(l => l.Count) != steps.Count())
        {
            throw new InvalidOperationException(
                "Circular dependency detected in pipeline steps");
        }

        return levels;
    }

    private static async Task ExecuteStepAsync(PipelineStep step, DeployingContext context)
    {
        try
        {
            await step.Action(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Step '{step.Name}' failed: {ex.Message}", ex);
        }
    }

    public override string ToString()
    {
        if (_steps.Count == 0)
        {
            return "Pipeline: (empty)";
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Pipeline with {_steps.Count} step(s):");

        foreach (var step in _steps)
        {
            sb.Append(CultureInfo.InvariantCulture, $"  - {step.Name}");

            if (step.Dependencies.Count > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $" [depends on: {string.Join(", ", step.Dependencies)}]");
            }

            if (step.RequiredBy.Count > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $" [required by: {string.Join(", ", step.RequiredBy)}]");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
