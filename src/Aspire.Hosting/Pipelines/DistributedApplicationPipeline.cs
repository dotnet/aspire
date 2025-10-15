// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Channels;
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
    /// Executes pipeline steps using a channel-based readiness scheduler with producer-consumer pattern.
    /// Steps are executed as soon as their dependencies are met, with automatic concurrency management.
    /// </summary>
    private static async Task ExecuteWithReadinessScheduler(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName,
        DeployingContext context)
    {
        // Build the dependency graph and validate no cycles
        var (indegrees, dependents) = BuildDependencyGraph(steps, stepsByName);

        // Create an unbounded channel for ready steps
        var readyChannel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        // Track exceptions and completion
        var exceptions = new ConcurrentQueue<Exception>();
        var completedCount = 0;

        // Enqueue all zero in-degree steps initially
        for (var i = 0; i < steps.Count; i++)
        {
            if (indegrees[i] == 0)
            {
                await readyChannel.Writer.WriteAsync(i).ConfigureAwait(false);
            }
        }

        // Producer-consumer: Read from channel and execute steps
        var maxConcurrency = Environment.ProcessorCount;
        var executionTasks = new Task[maxConcurrency];

        for (var i = 0; i < maxConcurrency; i++)
        {
            executionTasks[i] = Task.Run(async () =>
            {
                await foreach (var stepIndex in readyChannel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    var step = steps[stepIndex];
                    try
                    {
                        await ExecuteStepAsync(step, context).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                        // Signal failure - complete the channel
                        readyChannel.Writer.TryComplete();
                        return;
                    }

                    // Mark as completed and check if we're done
                    var completed = Interlocked.Increment(ref completedCount);

                    // Check which dependent steps are now ready (use index-based lookup)
                    var stepName = step.Name;
                    if (dependents.TryGetValue(stepName, out var dependentIndices))
                    {
                        foreach (var dependentIndex in dependentIndices)
                        {
                            // Decrement the in-degree and check if all dependencies are satisfied
                            if (Interlocked.Decrement(ref indegrees[dependentIndex]) == 0)
                            {
                                await readyChannel.Writer.WriteAsync(dependentIndex).ConfigureAwait(false);
                            }
                        }
                    }

                    // If all steps are done, complete the channel (check once per completion)
                    if (completed == steps.Count)
                    {
                        readyChannel.Writer.TryComplete();
                    }
                }
            });
        }

        // Wait for all execution tasks to complete
        await Task.WhenAll(executionTasks).ConfigureAwait(false);

        // Throw exceptions if any occurred
        if (!exceptions.IsEmpty)
        {
            var exceptionList = exceptions.ToList();
            if (exceptionList.Count == 1)
            {
                ExceptionDispatchInfo.Throw(exceptionList[0]);
            }
            else
            {
                throw new AggregateException(
                    $"Multiple pipeline steps failed: {string.Join(", ", exceptionList.Select(e => e.Message))}",
                    exceptionList);
            }
        }
    }

    /// <summary>
    /// Builds the dependency graph for the pipeline steps.
    /// </summary>
    /// <remarks>
    /// This method constructs a directed acyclic graph (DAG) representing step dependencies and validates
    /// that no circular dependencies exist. It uses Kahn's algorithm for topological sorting and cycle detection.
    /// 
    /// Example: For steps A → B → C (where → means "depends on"), the method produces:
    /// - indegrees: [0, 1, 1] (A has 0 dependencies, B depends on 1 step, C depends on 1 step)
    /// - dependents: {"A" → [1], "B" → [2], "C" → []} (A is required by step at index 1, etc.)
    /// 
    /// Diamond dependency example: A → B, A → C, B → D, C → D produces:
    /// - indegrees: [0, 1, 1, 2] (D has 2 dependencies: both B and C)
    /// - dependents: {"A" → [1, 2], "B" → [3], "C" → [3], "D" → []}
    /// </remarks>
    /// <returns>A tuple of (indegrees array, dependents dictionary)</returns>
    private static (int[] indegrees, Dictionary<string, List<int>> dependents) BuildDependencyGraph(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName)
    {
        // Arrays/dictionaries to track the dependency graph structure
        // - indegrees[i]: number of dependencies for step i (in-degree count)
        // - dependents["StepName"]: list of step indices that depend on "StepName"
        // - stepIndices["StepName"]: index of step in the steps list
        var indegrees = new int[steps.Count];
        var dependents = new Dictionary<string, List<int>>(steps.Count);
        var stepIndices = new Dictionary<string, int>(steps.Count);

        // Build step index mapping and initialize dependents dictionary
        // Example: ["A", "B", "C"] → stepIndices: {"A"→0, "B"→1, "C"→2}
        for (var i = 0; i < steps.Count; i++)
        {
            stepIndices[steps[i].Name] = i;
            dependents[steps[i].Name] = [];
        }

        // Process all the `RequiredBy` relationships and normalize to DependsOn
        // Example: If step "A" has RequiredBy="B", then "B" gets a dependency on "A"
        // This converts: A.RequiredBy("B") → B.DependsOn("A")
        foreach (var step in steps)
        {
            foreach (var requiredByStep in step.RequiredBySteps)
            {
                if (!stepsByName.TryGetValue(requiredByStep, out var requiredByStepObj))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredByStep}'");
                }

                // Add the dependency relationship
                requiredByStepObj.DependsOnSteps.Add(step.Name);
            }
        }

        // Build the graph based on DependsOn relationships
        // For each dependency, update:
        // 1. The dependents dictionary (reverse edges for efficient traversal)
        // 2. The indegrees array (count incoming edges)
        // Example: If B depends on A, then dependents["A"] includes B's index, and indegrees[B]++
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            foreach (var dependency in step.DependsOnSteps)
            {
                if (!stepIndices.TryGetValue(dependency, out var depIndex))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' depends on unknown step '{dependency}'");
                }

                dependents[dependency].Add(i);
                indegrees[i]++;
            }
        }

        // Cycle detection using Kahn's algorithm (topological sort)
        // Kahn's algorithm: repeatedly remove nodes with in-degree 0
        // If all nodes are removed, the graph is a DAG (no cycles)
        // If some nodes remain, they form a cycle
        var queue = new Queue<int>(steps.Count);
        var processedSteps = new HashSet<int>(steps.Count);

        // Initialize queue with all zero in-degree steps (steps with no dependencies)
        // Example: In A → B → C, only A has in-degree 0 initially
        for (var i = 0; i < steps.Count; i++)
        {
            if (indegrees[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        // Process steps in topological order
        // Each iteration removes a step and decrements the in-degree of its dependents
        while (queue.Count > 0)
        {
            var stepIndex = queue.Dequeue();
            processedSteps.Add(stepIndex);

            var stepName = steps[stepIndex].Name;
            if (dependents.TryGetValue(stepName, out var deps))
            {
                foreach (var depIndex in deps)
                {
                    // Decrement in-degree and enqueue if it becomes 0
                    // Note: We're modifying indegrees during cycle detection
                    // This is safe because we rebuild it before returning
                    if (--indegrees[depIndex] == 0)
                    {
                        queue.Enqueue(depIndex);
                    }
                }
            }
        }

        // If not all steps were processed, there's a cycle
        // Example: A → B → C → A would leave all three steps unprocessed
        if (processedSteps.Count != steps.Count)
        {
            var stepsInCycle = new List<string>(steps.Count - processedSteps.Count);
            for (var i = 0; i < steps.Count; i++)
            {
                if (!processedSteps.Contains(i))
                {
                    stepsInCycle.Add(steps[i].Name);
                }
            }

            throw new InvalidOperationException(
                $"Circular dependency detected in pipeline steps: {string.Join(", ", stepsInCycle)}");
        }

        // Rebuild indegrees array since we modified it during cycle detection
        // Simply count the dependencies for each step
        Array.Clear(indegrees);
        for (var i = 0; i < steps.Count; i++)
        {
            indegrees[i] = steps[i].DependsOnSteps.Count;
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
