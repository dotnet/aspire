// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using System.Collections.Concurrent;
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

        // Build dependency graph and execute with readiness-based scheduler
        await ExecuteWithReadinessScheduler(allSteps, stepsByName, context).ConfigureAwait(false);
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
    /// Executes pipeline steps using a readiness-based scheduler that starts steps as soon as their dependencies are met.
    /// </summary>
    private static async Task ExecuteWithReadinessScheduler(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName,
        DeployingContext context)
    {
        // Build the dependency graph and validate no cycles
        var (indegrees, dependents) = BuildDependencyGraph(steps, stepsByName);

        // Create step index mapping
        var stepIndices = new Dictionary<string, int>();
        for (int i = 0; i < steps.Count; i++)
        {
            stepIndices[steps[i].Name] = i;
        }

        // Track running tasks and exceptions
        var runningTasks = new HashSet<Task<int>>();
        var exceptions = new ConcurrentBag<Exception>();
        var completedSteps = new HashSet<int>();
        var completedStepsLock = new object();

        // Enqueue all zero in-degree steps initially
        var readySteps = new List<int>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (indegrees[i] == 0)
            {
                readySteps.Add(i);
            }
        }

        // Main execution loop
        while (completedSteps.Count < steps.Count || runningTasks.Count > 0)
        {
            // Start all ready steps
            foreach (var stepIndex in readySteps)
            {
                var step = steps[stepIndex];
                var task = ExecuteStepWithTrackingAsync(stepIndex, step, context, exceptions);
                runningTasks.Add(task);
            }
            readySteps.Clear();

            // If no tasks are running and no steps are ready, we have a problem
            if (runningTasks.Count == 0)
            {
                break;
            }

            // Wait for at least one task to complete
            var completedTask = await Task.WhenAny(runningTasks).ConfigureAwait(false);
            runningTasks.Remove(completedTask);

            var completedStepIndex = await completedTask.ConfigureAwait(false);

            lock (completedStepsLock)
            {
                completedSteps.Add(completedStepIndex);
            }

            // If a failure occurred, stop scheduling new steps but wait for running tasks
            if (!exceptions.IsEmpty)
            {
                // Wait for all running tasks to complete
                if (runningTasks.Count > 0)
                {
                    await Task.WhenAll(runningTasks).ConfigureAwait(false);
                }
                break;
            }

            // Check which dependent steps are now ready
            var completedStepName = steps[completedStepIndex].Name;
            if (dependents.TryGetValue(completedStepName, out var dependentIndices))
            {
                foreach (var dependentIndex in dependentIndices)
                {
                    // Decrement the in-degree and check if all dependencies are satisfied
                    if (Interlocked.Decrement(ref indegrees[dependentIndex]) == 0)
                    {
                        readySteps.Add(dependentIndex);
                    }
                }
            }
        }

        // Check for circular dependencies (defensive check)
        if (completedSteps.Count != steps.Count && exceptions.IsEmpty)
        {
            var uncompletedSteps = steps
                .Where((_, i) => !completedSteps.Contains(i))
                .Select(s => s.Name)
                .ToList();

            throw new InvalidOperationException(
                $"Circular dependency detected in pipeline steps: {string.Join(", ", uncompletedSteps)}");
        }

        // Throw exceptions if any occurred
        if (!exceptions.IsEmpty)
        {
            var exceptionList = exceptions.ToList();
            if (exceptionList.Count == 1)
            {
                ExceptionDispatchInfo.Capture(exceptionList[0]).Throw();
            }
            else
            {
                throw new AggregateException(
                    $"Multiple pipeline steps failed at the same level: {string.Join(", ", exceptionList.OfType<InvalidOperationException>().Select(e => e.Message))}",
                    exceptionList);
            }
        }
    }

    /// <summary>
    /// Executes a step and returns its index upon completion.
    /// </summary>
    private static async Task<int> ExecuteStepWithTrackingAsync(
        int stepIndex,
        PipelineStep step,
        DeployingContext context,
        ConcurrentBag<Exception> exceptions)
    {
        try
        {
            await ExecuteStepAsync(step, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        return stepIndex;
    }

    /// <summary>
    /// Builds the dependency graph for the pipeline steps.
    /// </summary>
    /// <returns>A tuple of (indegrees array, dependents dictionary)</returns>
    private static (int[] indegrees, Dictionary<string, List<int>> dependents) BuildDependencyGraph(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName)
    {
        var indegrees = new int[steps.Count];
        var dependents = new Dictionary<string, List<int>>();
        var stepIndices = new Dictionary<string, int>();

        for (int i = 0; i < steps.Count; i++)
        {
            stepIndices[steps[i].Name] = i;
            dependents[steps[i].Name] = [];
        }

        // Process all the `RequiredBy` relationships and normalize to DependsOn
        foreach (var step in steps)
        {
            foreach (var requiredByStep in step.RequiredBySteps)
            {
                if (!stepsByName.TryGetValue(requiredByStep, out var requiredByStepObj))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredByStep}'");
                }

                if (!requiredByStepObj.DependsOnSteps.Contains(step.Name))
                {
                    requiredByStepObj.DependsOnSteps.Add(step.Name);
                }
            }
        }

        // Build the graph based on DependsOn relationships
        foreach (var step in steps)
        {
            var stepIndex = stepIndices[step.Name];

            foreach (var dependency in step.DependsOnSteps)
            {
                if (!stepIndices.TryGetValue(dependency, out var depIndex))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' depends on unknown step '{dependency}'");
                }

                dependents[dependency].Add(stepIndex);
                indegrees[stepIndex]++;
            }
        }

        // Cycle detection using topological sort
        var queue = new Queue<int>();
        var tempIndegrees = (int[])indegrees.Clone();

        for (int i = 0; i < steps.Count; i++)
        {
            if (tempIndegrees[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        var processedCount = 0;
        while (queue.Count > 0)
        {
            var stepIndex = queue.Dequeue();
            processedCount++;

            var stepName = steps[stepIndex].Name;
            if (dependents.TryGetValue(stepName, out var deps))
            {
                foreach (var depIndex in deps)
                {
                    if (--tempIndegrees[depIndex] == 0)
                    {
                        queue.Enqueue(depIndex);
                    }
                }
            }
        }

        if (processedCount != steps.Count)
        {
            var processedIndices = new HashSet<int>();
            queue.Clear();
            tempIndegrees = (int[])indegrees.Clone();

            for (int i = 0; i < steps.Count; i++)
            {
                if (tempIndegrees[i] == 0)
                {
                    queue.Enqueue(i);
                }
            }

            while (queue.Count > 0)
            {
                var idx = queue.Dequeue();
                processedIndices.Add(idx);

                if (dependents.TryGetValue(steps[idx].Name, out var deps))
                {
                    foreach (var depIdx in deps)
                    {
                        if (--tempIndegrees[depIdx] == 0)
                        {
                            queue.Enqueue(depIdx);
                        }
                    }
                }
            }

            var stepsInCycle = steps
                .Where((s, i) => !processedIndices.Contains(i))
                .Select(s => s.Name)
                .ToList();

            throw new InvalidOperationException(
                $"Circular dependency detected in pipeline steps: {string.Join(", ", stepsInCycle)}");
        }

        return (indegrees, dependents);
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
