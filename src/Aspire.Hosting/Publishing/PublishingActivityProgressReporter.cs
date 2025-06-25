// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Aspire.Hosting.Backchannel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the completion state of a publishing activity (task, step, or top-level operation).
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum CompletionState
{
    /// <summary>
    /// The task is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// The task completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The task completed with warnings.
    /// </summary>
    CompletedWithWarning,

    /// <summary>
    /// The task completed with an error.
    /// </summary>
    CompletedWithError
}

/// <summary>
/// Represents a publishing step, which can contain multiple tasks.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingStep : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, PublishingTask> _tasks = new();

    internal PublishingStep(string id, string title)
    {
        Id = id;
        Title = title;
    }

    /// <summary>
    /// Unique Id of the step.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// The title of the publishing step.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// The completion state of the step. When null, the state is automatically aggregated from child tasks.
    /// When explicitly set, the specified state overrides the aggregated state.
    /// </summary>
    public CompletionState CompletionState
    {
        get => _completionState ?? CalculateAggregatedState();
        internal set => _completionState = value;
    }
    
    private CompletionState? _completionState;

    /// <summary>
    /// The completion text for the step.
    /// </summary>
    public string CompletionText { get; internal set; } = string.Empty;

    /// <summary>
    /// The collection of child tasks belonging to this step.
    /// </summary>
    public IReadOnlyDictionary<string, PublishingTask> Tasks => _tasks;

    /// <summary>
    /// The progress reporter that created this step.
    /// </summary>
    internal IPublishingActivityProgressReporter? Reporter { get; set; }

    /// <summary>
    /// Adds a task to this step.
    /// </summary>
    internal void AddTask(PublishingTask task)
    {
        _tasks.TryAdd(task.Id, task);
    }

    /// <summary>
    /// Recalculates the completion state based on child tasks.
    /// </summary>
    internal CompletionState CalculateAggregatedState()
    {
        if (_tasks.IsEmpty)
        {
            return CompletionState.InProgress;
        }

        var maxState = CompletionState.InProgress;
        foreach (var task in _tasks.Values)
        {
            if ((int)task.CompletionState > (int)maxState)
            {
                maxState = task.CompletionState;
            }
        }
        return maxState;
    }

    /// <summary>
    /// Creates a new task within this step.
    /// </summary>
    /// <param name="statusText">The initial status text for the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created task.</returns>
    public async Task<PublishingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot create task: Reporter is not set.");
        }

        return await Reporter.CreateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the step and aggregates the final completion state from all child tasks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is null)
        {
            return;
        }

        // Use the current completion state (which handles aggregation if not explicitly set)
        var finalState = CompletionState;
        
        // Only set completion text if it has not been explicitly set
        var completionText = string.IsNullOrEmpty(CompletionText) 
            ? finalState switch
            {
                CompletionState.Completed => $"{Title} completed successfully",
                CompletionState.CompletedWithWarning => $"{Title} completed with warnings",
                CompletionState.CompletedWithError => $"{Title} completed with errors",
                _ => $"{Title} completed"
            }
            : CompletionText;

        await Reporter.CompleteStepAsync(this, completionText, finalState, CancellationToken.None).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingTask : IAsyncDisposable
{
    internal PublishingTask(string id, string stepId, string statusText, PublishingStep parentStep)
    {
        Id = id;
        StepId = stepId;
        StatusText = statusText;
        ParentStep = parentStep;
    }
    /// <summary>
    /// Unique Id of the task.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// The identifier of the step this task belongs to.
    /// </summary>
    public string StepId { get; private set; }

    /// <summary>
    /// Reference to the parent step this task belongs to.
    /// </summary>
    public PublishingStep ParentStep { get; internal set; }

    /// <summary>
    /// The current status text of the task.
    /// </summary>
    public string StatusText { get; internal set; }

    /// <summary>
    /// The completion state of the task.
    /// </summary>
    public CompletionState CompletionState { get; internal set; } = CompletionState.InProgress;

    /// <summary>
    /// Optional completion message for the task.
    /// </summary>
    public string CompletionMessage { get; internal set; } = string.Empty;

    /// <summary>
    /// The progress reporter that created this task.
    /// </summary>
    internal IPublishingActivityProgressReporter? Reporter { get; set; }

    /// <summary>
    /// Updates the status text of this task.
    /// </summary>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot update task: Reporter is not set.");
        }

        await Reporter.UpdateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as completed successfully.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, CompletionState.Completed, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as completed with warnings.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteWithWarningAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithWarning, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as failed with an error.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task FailAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot fail task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithError, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the task, completing it successfully if not already completed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is null || CompletionState != CompletionState.InProgress)
        {
            return;
        }

        // Auto-complete with success if not already completed
        await CompleteAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }
}

/// <summary>
/// Interface for reporting publishing activity progress.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingActivityProgressReporter
{
    /// <summary>
    /// Creates a new publishing step with the specified ID and title.
    /// </summary>
    /// <param name="title">The title of the publishing step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The publishing step</returns>
    /// <exception cref="InvalidOperationException">Thrown when a step with the same ID already exists.</exception>
    Task<PublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new publishing task tied to a step.
    /// </summary>
    /// <param name="step">The step this task belongs to.</param>
    /// <param name="statusText">The initial status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The publishing task</returns>
    /// <exception cref="InvalidOperationException">Thrown when the step does not exist or is already complete.</exception>
    Task<PublishingTask> CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a publishing step with the specified completion text and state.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="completionState">The completion state for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status text of an existing publishing task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent step is already complete.</exception>
    Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a publishing task with the specified completion state and optional completion message.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="completionState">The completion state.</param>
    /// <param name="completionMessage">Optional completion message that will appear as a dimmed child message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent step is already complete.</exception>
    Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that the entire publishing process has completed.
    /// </summary>
    /// <param name="success">Whether the publishing process completed successfully.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CompletePublishAsync(bool success, CancellationToken cancellationToken);
}

internal sealed class PublishingActivityProgressReporter : IPublishingActivityProgressReporter
{
    private readonly ConcurrentDictionary<string, PublishingStep> _steps = new();

#pragma warning disable CS0618 // Type or member is obsolete - we're maintaining backward compatibility
    private static string ToBackchannelCompletionState(CompletionState state) => state switch
    {
        CompletionState.InProgress => "InProgress",
        CompletionState.Completed => "Completed",
        CompletionState.CompletedWithWarning => "CompletedWithWarning",
        CompletionState.CompletedWithError => "CompletedWithError",
        _ => "InProgress"
    };

    public async Task<PublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken)
    {
        var step = new PublishingStep(Guid.NewGuid().ToString(), title);
        step.Reporter = this;
        _steps.TryAdd(step.Id, step);

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = step.Title,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = null
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        return step;
    }

    public async Task<PublishingTask> CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(step.Id, out var parentStep))
        {
            throw new InvalidOperationException($"Step with ID '{step.Id}' does not exist.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot create task for step '{step.Id}' because the step is already complete.");
            }
        }

        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText, parentStep);
        task.Reporter = this;
        
        // Add task to parent step
        parentStep.AddTask(task);

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = statusText,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = step.Id
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        return task;
    }

    public async Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken = default)
    {
        lock (step)
        {
            step.CompletionState = completionState;
            step.CompletionText = completionText;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = completionText,
                CompletionState = ToBackchannelCompletionState(completionState),
                StepId = null
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);

        // Remove the completed step to prevent further updates.
        _steps.TryRemove(step.Id, out _);
    }

    public async Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            throw new InvalidOperationException($"Parent step with ID '{task.StepId}' does not exist.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot update task '{task.Id}' because its parent step '{task.StepId}' is already complete.");
            }

            task.StatusText = statusText;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = statusText,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = task.StepId
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            throw new InvalidOperationException($"Parent step with ID '{task.StepId}' does not exist.");
        }

        if (task.CompletionState != CompletionState.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete task '{task.Id}' with state '{task.CompletionState}'. Only 'InProgress' tasks can be completed.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot complete task '{task.Id}' because its parent step '{task.StepId}' is already complete.");
            }

            task.CompletionState = completionState;
            task.CompletionMessage = completionMessage ?? string.Empty;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = task.StatusText,
                CompletionState = ToBackchannelCompletionState(completionState),
                StepId = task.StepId,
                CompletionMessage = completionMessage
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompletePublishAsync(bool success, CancellationToken cancellationToken)
    {
        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.PublishComplete,
            Data = new PublishingActivityData
            {
                Id = PublishingActivityTypes.PublishComplete,
                StatusText = success ? "Publishing completed successfully" : "Publishing completed with errors",
                CompletionState = ToBackchannelCompletionState(success ? CompletionState.Completed : CompletionState.CompletedWithError)
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    internal Channel<PublishingActivity> ActivityItemUpdated { get; } = Channel.CreateUnbounded<PublishingActivity>();
#pragma warning restore CS0618 // Type or member is obsolete
}
