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

/// <summary>
/// Represents a failed pipeline step with its associated exception.
/// </summary>
internal sealed class PipelineStepFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStepFailure"/> class.
    /// </summary>
    /// <param name="stepName">The name of the failed step.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public PipelineStepFailure(string stepName, Exception exception)
    {
        StepName = stepName;
        Exception = exception;
    }

    /// <summary>
    /// Gets the name of the failed step.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    /// Gets the exception that caused the step to fail.
    /// </summary>
    public Exception Exception { get; }
}

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class DistributedApplicationPipeline : IDistributedApplicationPipeline
{
    private readonly List<PipelineStep> _steps = [];
    private readonly List<PipelineStepFailure> _failedSteps = [];

    public bool HasSteps => _steps.Count > 0;

    /// <summary>
    /// Gets the list of steps that failed during the last execution.
    /// Only includes steps that threw exceptions during execution, not steps that were skipped due to dependency failures.
    /// This list is cleared at the start of each execution.
    /// </summary>
    public IReadOnlyList<PipelineStepFailure> FailedSteps => _failedSteps;

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
        // Clear failed steps from any previous execution
        _failedSteps.Clear();

        var allSteps = _steps.Concat(CollectStepsFromAnnotations(context)).ToList();

        if (allSteps.Count == 0)
        {
            return;
        }

        ValidateSteps(allSteps);

        var stepsByName = allSteps.ToDictionary(s => s.Name, StringComparer.Ordinal);

        // Build dependency graph and execute with readiness-based scheduler
        await ExecuteStepsAsTaskDag(allSteps, stepsByName, context).ConfigureAwait(false);
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
        var stepNames = new HashSet<string>(StringComparer.Ordinal);

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
    /// Executes pipeline steps by building a Task DAG where each step waits on its dependencies.
    /// Uses CancellationToken to stop remaining work when any step fails.
    /// </summary>
    private async Task ExecuteStepsAsTaskDag(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName,
        DeployingContext context)
    {
        // Validate no cycles exist in the dependency graph
        ValidateDependencyGraph(steps, stepsByName);

        // Create a TaskCompletionSource for each step
        var stepCompletions = new Dictionary<string, TaskCompletionSource>(steps.Count, StringComparer.Ordinal);
        foreach (var step in steps)
        {
            stepCompletions[step.Name] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // Execute a step after its dependencies complete
        async Task ExecuteStepWithDependencies(PipelineStep step)
        {
            var stepTcs = stepCompletions[step.Name];

            // Wait for all dependencies to complete (will throw if any dependency failed)
            if (step.DependsOnSteps.Count > 0)
            {
                try
                {
                    var depTasks = step.DependsOnSteps.Select(depName => stepCompletions[depName].Task);
                    await Task.WhenAll(depTasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Find all dependencies that failed
                    var failedDeps = step.DependsOnSteps
                        .Where(depName => stepCompletions[depName].Task.IsFaulted)
                        .ToList();
                    
                    var message = failedDeps.Count > 0
                        ? $"Step '{step.Name}' cannot run because {(failedDeps.Count == 1 ? "dependency" : "dependencies")} {string.Join(", ", failedDeps.Select(d => $"'{d}'"))} failed"
                        : $"Step '{step.Name}' cannot run because a dependency failed";
                    
                    // Wrap the dependency failure with context about this step
                    var wrappedException = new InvalidOperationException(message, ex);
                    stepTcs.TrySetException(wrappedException);
                    return;
                }
            }

            try
            {
                await ExecuteStepAsync(step, context).ConfigureAwait(false);

                stepTcs.TrySetResult();
            }
            catch (Exception ex)
            {
                // Execution failure - mark as failed and re-throw so it's counted
                stepTcs.TrySetException(ex);
                throw;
            }
        }

        // Start all steps (they'll wait on their dependencies internally)
        var allStepTasks = new Task[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            allStepTasks[i] = Task.Run(() => ExecuteStepWithDependencies(step));
        }

        // Wait for all steps to complete (or fail)
        try
        {
            await Task.WhenAll(allStepTasks).ConfigureAwait(false);
        }
        catch
        {
            // Collect all failed steps and their names
            var failures = allStepTasks
                .Where(t => t.IsFaulted)
                .Select(t => t.Exception!)
                .SelectMany(ae => ae.InnerExceptions)
                .ToList();

            // Track failed steps for reporting
            for (var i = 0; i < allStepTasks.Length; i++)
            {
                if (allStepTasks[i].IsFaulted)
                {
                    var stepName = steps[i].Name;
                    var taskException = allStepTasks[i].Exception;
                    if (taskException is not null)
                    {
                        var stepException = taskException.InnerExceptions.FirstOrDefault() ?? taskException;
                        _failedSteps.Add(new PipelineStepFailure(stepName, stepException));
                    }
                }
            }

            if (failures.Count > 1)
            {
                // Match failures to steps to get their names
                var failedStepNames = new List<string>();
                for (var i = 0; i < allStepTasks.Length; i++)
                {
                    if (allStepTasks[i].IsFaulted)
                    {
                        failedStepNames.Add(steps[i].Name);
                    }
                }

                var message = failedStepNames.Count > 0
                    ? $"Multiple pipeline steps failed: {string.Join(", ", failedStepNames.Distinct())}"
                    : "Multiple pipeline steps failed.";

                throw new AggregateException(message, failures);
            }

            // Single failure - just rethrow
            throw;
        }
    }

    /// <summary>
    /// Represents the visitation state of a step during cycle detection.
    /// </summary>
    private enum VisitState
    {
        /// <summary>
        /// The step has not been visited yet.
        /// </summary>
        Unvisited,
        
        /// <summary>
        /// The step is currently being visited (on the current DFS path).
        /// </summary>
        Visiting,
        
        /// <summary>
        /// The step has been fully visited (all descendants explored).
        /// </summary>
        Visited
    }

    /// <summary>
    /// Validates that the pipeline steps form a directed acyclic graph (DAG) with no circular dependencies.
    /// </summary>
    /// <remarks>
    /// Uses depth-first search (DFS) to detect cycles. A cycle exists if we encounter a node that is
    /// currently being visited (in the Visiting state), meaning we've found a back edge in the graph.
    /// 
    /// Example: A → B → C is valid (no cycle)
    /// Example: A → B → C → A is invalid (cycle detected)
    /// Example: A → B, A → C, B → D, C → D is valid (diamond dependency, no cycle)
    /// </remarks>
    private static void ValidateDependencyGraph(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName)
    {
        // Process all RequiredBy relationships and normalize to DependsOn
        foreach (var step in steps)
        {
            foreach (var requiredByStep in step.RequiredBySteps)
            {
                if (!stepsByName.TryGetValue(requiredByStep, out var requiredByStepObj))
                {
                    throw new InvalidOperationException(
                        $"Step '{step.Name}' is required by unknown step '{requiredByStep}'");
                }

                requiredByStepObj.DependsOnSteps.Add(step.Name);
            }
        }

        var visitStates = new Dictionary<string, VisitState>(steps.Count, StringComparer.Ordinal);
        foreach (var step in steps)
        {
            visitStates[step.Name] = VisitState.Unvisited;
        }

        // DFS to detect cycles
        void DetectCycles(string stepName, Stack<string> path)
        {
            var state = visitStates[stepName];

            if (state == VisitState.Visiting) // Currently visiting - cycle detected!
            {
                var cycle = path.Reverse().SkipWhile(s => s != stepName).Append(stepName);
                throw new InvalidOperationException(
                    $"Circular dependency detected in pipeline steps: {string.Join(" → ", cycle)}");
            }

            if (state == VisitState.Visited) // Already fully visited - no need to check again
            {
                return;
            }

            visitStates[stepName] = VisitState.Visiting;
            path.Push(stepName);

            if (stepsByName.TryGetValue(stepName, out var step))
            {
                foreach (var dependency in step.DependsOnSteps)
                {
                    DetectCycles(dependency, path);
                }
            }

            path.Pop();
            visitStates[stepName] = VisitState.Visited;
        }

        // Check each step for cycles
        var path = new Stack<string>();
        foreach (var step in steps)
        {
            if (visitStates[step.Name] == VisitState.Unvisited)
            {
                DetectCycles(step.Name, path);
            }
        }
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
