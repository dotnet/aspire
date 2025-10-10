// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

internal sealed class DistributedApplicationPipeline : IDistributedApplicationPipeline
{
    private readonly List<PipelineStep> _steps = [];

    public void AddStep(string name,
        Func<DeployingContext, Task> action,
        object? dependsOn = null,
        object? requiredBy = null)
    {
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

        var registry = new PipelineRegistry(allSteps);

        var levels = ResolveDependencies(allSteps, registry);

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
                foreach (var step in annotation.CreateSteps(context))
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
        IPipelineRegistry registry)
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

                var requiredByStepObj = registry.GetStep(requiredByStep);
                if (requiredByStepObj != null && !requiredByStepObj.Dependencies.Contains(step.Name))
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
                var step = registry.GetStep(stepName)!;
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

    private sealed class PipelineRegistry(IEnumerable<PipelineStep> steps) : IPipelineRegistry
    {
        private readonly Dictionary<string, PipelineStep> _stepsByName = steps.ToDictionary(s => s.Name);

        public IEnumerable<PipelineStep> GetAllSteps() => _stepsByName.Values;

        public PipelineStep? GetStep(string name) =>
            _stepsByName.TryGetValue(name, out var step) ? step : null;
    }
}
