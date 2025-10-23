// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Pipelines;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class DistributedApplicationPipeline : IDistributedApplicationPipeline
{
    private readonly List<PipelineStep> _steps = [];
    private readonly List<Func<PipelineConfigurationContext, Task>> _configurationCallbacks = [];

    public bool HasSteps => _steps.Count > 0;

    public void AddStep(string name,
        Func<PipelineStepContext, Task> action,
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

    public void AddPipelineConfiguration(Func<PipelineConfigurationContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _configurationCallbacks.Add(callback);
    }

    public async Task ExecuteAsync(PipelineContext context)
    {
        var (annotationSteps, stepToResourceMap) = await CollectStepsFromAnnotationsAsync(context).ConfigureAwait(false);
        var allSteps = _steps.Concat(annotationSteps).ToList();

        // Execute configuration callbacks even if there are no steps
        // This allows callbacks to run validation or other logic
        await ExecuteConfigurationCallbacksAsync(context, allSteps, stepToResourceMap).ConfigureAwait(false);

        if (allSteps.Count == 0)
        {
            return;
        }

        ValidateSteps(allSteps);

        var (stepsToExecute, stepsByName) = FilterStepsForExecution(allSteps, context);

        // Build dependency graph and execute with readiness-based scheduler
        await ExecuteStepsAsTaskDag(stepsToExecute, stepsByName, context).ConfigureAwait(false);
    }

    private static (List<PipelineStep> StepsToExecute, Dictionary<string, PipelineStep> StepsByName) FilterStepsForExecution(
        List<PipelineStep> allSteps,
        PipelineContext context)
    {
        var publishingOptions = context.Services.GetService<Microsoft.Extensions.Options.IOptions<Publishing.PublishingOptions>>();
        var stepName = publishingOptions?.Value.Step;
        var tag = publishingOptions?.Value.Tag;
        var allStepsByName = allSteps.ToDictionary(s => s.Name, StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(stepName))
        {
            if (!allStepsByName.TryGetValue(stepName, out var targetStep))
            {
                var availableSteps = string.Join(", ", allSteps.Select(s => $"'{s.Name}'"));
                throw new InvalidOperationException(
                    $"Step '{stepName}' not found in pipeline. Available steps: {availableSteps}");
            }

            var stepsToExecute = ComputeTransitiveDependencies(targetStep, allStepsByName);
            stepsToExecute.Add(targetStep);
            var filteredStepsByName = stepsToExecute.ToDictionary(s => s.Name, StringComparer.Ordinal);
            return (stepsToExecute, filteredStepsByName);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var stepsWithTag = allSteps.Where(s => s.Tags.Contains(tag)).ToList();
            if (stepsWithTag.Count == 0)
            {
                var availableTags = allSteps.SelectMany(s => s.Tags).Distinct().ToList();
                var tagsMessage = availableTags.Count > 0
                    ? $" Available tags: {string.Join(", ", availableTags.Select(t => $"'{t}'"))}"
                    : " No tags found on any pipeline steps.";
                throw new InvalidOperationException(
                    $"No steps found with tag '{tag}'.{tagsMessage}");
            }

            // Compute transitive dependencies for all steps with the specified tag
            var stepsToExecute = new HashSet<PipelineStep>();
            foreach (var step in stepsWithTag)
            {
                var dependencies = ComputeTransitiveDependencies(step, allStepsByName);
                foreach (var dep in dependencies)
                {
                    stepsToExecute.Add(dep);
                }
                stepsToExecute.Add(step);
            }

            var filteredSteps = stepsToExecute.ToList();
            var filteredStepsByName = filteredSteps.ToDictionary(s => s.Name, StringComparer.Ordinal);
            return (filteredSteps, filteredStepsByName);
        }

        return (allSteps, allStepsByName);
    }

    private static List<PipelineStep> ComputeTransitiveDependencies(
        PipelineStep step,
        Dictionary<string, PipelineStep> stepsByName)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<PipelineStep>();

        void Visit(string stepName)
        {
            if (!visited.Add(stepName))
            {
                return;
            }

            if (!stepsByName.TryGetValue(stepName, out var currentStep))
            {
                return;
            }

            foreach (var dependency in currentStep.DependsOnSteps)
            {
                Visit(dependency);
            }

            result.Add(currentStep);
        }

        foreach (var dependency in step.DependsOnSteps)
        {
            Visit(dependency);
        }

        return result;
    }

    private static async Task<(List<PipelineStep> Steps, Dictionary<PipelineStep, IResource> StepToResourceMap)> CollectStepsFromAnnotationsAsync(PipelineContext context)
    {
        var steps = new List<PipelineStep>();
        var stepToResourceMap = new Dictionary<PipelineStep, IResource>();

        foreach (var resource in context.Model.Resources)
        {
            var annotations = resource.Annotations
                .OfType<PipelineStepAnnotation>();

            foreach (var annotation in annotations)
            {
                var factoryContext = new PipelineStepFactoryContext
                {
                    PipelineContext = context,
                    Resource = resource
                };

                var annotationSteps = await annotation.CreateStepsAsync(factoryContext).ConfigureAwait(false);
                foreach (var step in annotationSteps)
                {
                    steps.Add(step);
                    stepToResourceMap[step] = resource;
                }
            }
        }

        return (steps, stepToResourceMap);
    }

    private async Task ExecuteConfigurationCallbacksAsync(
        PipelineContext pipelineContext,
        List<PipelineStep> allSteps,
        Dictionary<PipelineStep, IResource> stepToResourceMap)
    {
        // Collect callbacks from the pipeline itself
        var callbacks = new List<Func<PipelineConfigurationContext, Task>>();
        
        callbacks.AddRange(_configurationCallbacks);

        // Collect callbacks from resource annotations
        foreach (var resource in pipelineContext.Model.Resources)
        {
            var annotations = resource.Annotations.OfType<PipelineConfigurationAnnotation>();
            foreach (var annotation in annotations)
            {
                callbacks.Add(annotation.Callback);
            }
        }

        // Execute all callbacks
        if (callbacks.Count > 0)
        {
            var configContext = new PipelineConfigurationContext
            {
                Services = pipelineContext.Services,
                Steps = allSteps.AsReadOnly(),
                Model = pipelineContext.Model,
                StepToResourceMap = stepToResourceMap
            };

            foreach (var callback in callbacks)
            {
                await callback(configContext).ConfigureAwait(false);
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
    private static async Task ExecuteStepsAsTaskDag(
        List<PipelineStep> steps,
        Dictionary<string, PipelineStep> stepsByName,
        PipelineContext context)
    {
        // Validate no cycles exist in the dependency graph
        ValidateDependencyGraph(steps, stepsByName);

        // Create a linked CancellationTokenSource that will be cancelled when any step fails
        // or when the original context token is cancelled
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        // Store the original token and set the linked token on the context
        var originalToken = context.CancellationToken;
        context.CancellationToken = linkedCts.Token;

        try
        {
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
                        var depTasks = step.DependsOnSteps
                            .Where(stepCompletions.ContainsKey)
                            .Select(depName => stepCompletions[depName].Task);
                        await Task.WhenAll(depTasks).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Find all dependencies that failed
                        var failedDeps = step.DependsOnSteps
                            .Where(depName => stepCompletions.ContainsKey(depName) && stepCompletions[depName].Task.IsFaulted)
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
                    var activityReporter = context.Services.GetRequiredService<IPipelineActivityReporter>();
                    var publishingStep = await activityReporter.CreateStepAsync(step.Name, context.CancellationToken).ConfigureAwait(false);

                    await using (publishingStep.ConfigureAwait(false))
                    {
                        try
                        {
                            var stepContext = new PipelineStepContext
                            {
                                PipelineContext = context,
                                ReportingStep = publishingStep
                            };

                            PipelineLoggerProvider.CurrentLogger = stepContext.Logger;

                            await ExecuteStepAsync(step, stepContext).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // Report the failure to the activity reporter before disposing
                            await publishingStep.FailAsync(ex.Message, CancellationToken.None).ConfigureAwait(false);
                            throw;
                        }
                        finally
                        {
                            PipelineLoggerProvider.CurrentLogger = NullLogger.Instance;
                        }
                    }

                    stepTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    // Execution failure - mark as failed, cancel all other work, and re-throw
                    stepTcs.TrySetException(ex);

                    // Cancel all remaining work
                    try
                    {
                        linkedCts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore cancellation errors
                    }

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
        finally
        {
            // Restore the original token
            context.CancellationToken = originalToken;
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
                    continue;
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
            if (!visitStates.TryGetValue(stepName, out var state))
            {
                return;
            }

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

    private static async Task ExecuteStepAsync(PipelineStep step, PipelineStepContext stepContext)
    {
        try
        {
            await step.Action(stepContext).ConfigureAwait(false);
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
