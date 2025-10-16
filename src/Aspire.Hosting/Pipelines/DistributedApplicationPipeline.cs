// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class DistributedApplicationPipeline : IDistributedApplicationPipeline
{
    private readonly List<PipelineStep> _steps = [];

    public bool HasSteps => _steps.Count > 0;

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
            step.RequiredBy(stepName);
        }
        else if (requiredBy is IEnumerable<string> stepNames)
        {
            foreach (var name in stepNames)
            {
                step.RequiredBy(name);
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
            var tasks = level.Select(step => ExecuteStepAsync(step, context)).ToList();
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // Collect all exceptions from failed tasks
                var exceptions = tasks
                    .Where(t => t.IsFaulted)
                    .SelectMany(t => t.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>())
                    .ToList();

                if (exceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
                }
                else if (exceptions.Count > 1)
                {
                    throw new AggregateException(
                        $"Multiple pipeline steps failed at the same level: {string.Join(", ", exceptions.OfType<InvalidOperationException>().Select(e => e.Message))}",
                        exceptions);
                }

                throw;
            }
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
            foreach (var dependency in step.DependsOnSteps)
            {
                if (!stepNames.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' depends on unknown step '{dependency}'");
                }
            }

            foreach (var requiredBy in step.RequiredBySteps)
            {
                if (!stepNames.Contains(requiredBy))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredBy}'");
                }
            }
        }
    }

    /// <summary>
    /// Resolves the dependencies among the steps and organizes them into levels for execution.
    /// </summary>
    /// <param name="steps">The complete set of pipeline steps populated from annotations and the builder</param>
    /// <param name="stepsByName">A dictionary mapping step names to their corresponding step objects</param>
    /// <returns>A list of lists where each list contains the steps to be executed at the same level</returns>
    private static List<List<PipelineStep>> ResolveDependencies(
        IEnumerable<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName)
    {
        // Initial a graph that represents a step and its dependencies
        // and an inDegree map to count the number of dependencies that
        // each step has.
        var graph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        foreach (var step in steps)
        {
            graph[step.Name] = [];
            inDegree[step.Name] = 0;
        }

        // Process all the `RequiredBy` relationships in the graph and adds
        // the each `RequiredBy` step to the DependsOn list of the step that requires it.
        foreach (var step in steps)
        {
            foreach (var requiredByStep in step.RequiredBySteps)
            {
                if (!graph.ContainsKey(requiredByStep))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredByStep}'");
                }

                if (stepsByName.TryGetValue(requiredByStep, out var requiredByStepObj) &&
                    !requiredByStepObj.DependsOnSteps.Contains(step.Name))
                {
                    requiredByStepObj.DependsOnSteps.Add(step.Name);
                }
            }
        }

        // Now that the `DependsOn` lists are fully populated, we can build the graph
        // and the inDegree map based only on the DependOnSteps list.
        foreach (var step in steps)
        {
            foreach (var dependency in step.DependsOnSteps)
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

        // Perform a topological sort to determine the levels of execution and
        // initialize a queue with all steps that have no dependencies (inDegree of 0)
        // and can be executed immediately as part of the first level.
        var levels = new List<List<PipelineStep>>();
        var queue = new Queue<string>(
            inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key)
        );

        // Process the queue until all steps have been organized into levels.
        // We start with the steps that have no dependencies and then iterate
        // through all the steps that depend on them to build out the graph
        // until no more steps are available to process.
        while (queue.Count > 0)
        {
            var currentLevel = new List<PipelineStep>();
            var levelSize = queue.Count;

            for (var i = 0; i < levelSize; i++)
            {
                var stepName = queue.Dequeue();
                var step = stepsByName[stepName];
                currentLevel.Add(step);

                // For each dependent step, reduce its inDegree by 1
                // in each iteration since its dependencies have been
                // processed. Once a dependent step has an inDegree
                // of 0, it means all its dependencies have been
                // processed and it can be added to the queue so we
                // can process the next level of dependencies.
                foreach (var dependent in graph[stepName])
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            // Exhausting the queue means that we've resolved all
            // steps that can run in parallel.
            levels.Add(currentLevel);
        }

        // If the total number of steps in all levels does not equal
        // the total number of steps in the pipeline, it indicates that
        // there is a circular dependency in the graph. Steps are enqueued
        // for processing into levels above when all their dependencies are
        // resolved. When a cycle exists, the degrees of the steps in the cycle
        // will never reach zero and won't be enqueued for processing so the
        // total number of processed steps will be less than the total number
        // of steps in the pipeline.
        if (levels.Sum(l => l.Count) != steps.Count())
        {
            var processedSteps = new HashSet<string>(levels.SelectMany(l => l.Select(s => s.Name)));
            var stepsInCycle = steps.Where(s => !processedSteps.Contains(s.Name)).Select(s => s.Name).ToList();

            throw new InvalidOperationException(
                $"Circular dependency detected in pipeline steps: {string.Join(", ", stepsInCycle)}");
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
            var exceptionInfo = ExceptionDispatchInfo.Capture(ex);
            throw new InvalidOperationException(
                $"Step '{step.Name}' failed: {ex.Message}", exceptionInfo.SourceException);
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

            if (step.DependsOnSteps.Count > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $" [depends on: {string.Join(", ", step.DependsOnSteps)}]");
            }

            if (step.RequiredBySteps.Count > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $" [required by: {string.Join(", ", step.RequiredBySteps)}]");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
