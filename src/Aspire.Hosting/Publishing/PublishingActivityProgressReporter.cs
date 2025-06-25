// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Aspire.Hosting.Backchannel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the completion state of a publishing step or task.
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
    internal PublishingStep(string id, string title)
    {
        Id = id;
        Title = title;
        Tasks = new List<PublishingTask>();
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
    /// The completion state of the step.
    /// </summary>
    public CompletionState CompletionState { get; internal set; } = CompletionState.InProgress;

    /// <summary>
    /// The completion text for the step.
    /// </summary>
    public string CompletionText { get; internal set; } = string.Empty;

    /// <summary>
    /// The list of tasks that belong to this step.
    /// </summary>
    public IReadOnlyList<PublishingTask> Tasks { get; }

    /// <summary>
    /// The list of tasks that belong to this step (internal for modification).
    /// </summary>
    internal List<PublishingTask> TasksList => (List<PublishingTask>)Tasks;

    /// <summary>
    /// The progress reporter that created this step.
    /// </summary>
    internal IPublishingActivityProgressReporter? Reporter { get; set; }

    /// <summary>
    /// Marks the step for warning completion if a child task has an error.
    /// </summary>
    internal void MarkForWarningCompletion()
    {
        if (CompletionState == CompletionState.InProgress)
        {
            CompletionState = CompletionState.CompletedWithWarning;
        }
    }

    /// <summary>
    /// Completes the step automatically when disposed if not already completed.
    /// Also completes all child tasks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is not null && CompletionState == CompletionState.InProgress)
        {
            // Complete all child tasks first
            foreach (var task in Tasks.ToList())
            {
                if (task.CompletionState == CompletionState.InProgress)
                {
                    await Reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None).ConfigureAwait(false);
                }
            }

            // Complete the step with the current completion state or default to Completed
            var completionState = CompletionState == CompletionState.InProgress ? CompletionState.Completed : CompletionState;
            await Reporter.CompleteStepAsync(this, Title, completionState, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingTask : IAsyncDisposable
{
    internal PublishingTask(string id, string stepId, string statusText)
    {
        Id = id;
        StepId = stepId;
        StatusText = statusText;
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
    /// The parent step this task belongs to.
    /// </summary>
    public PublishingStep? ParentStep { get; internal set; }

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
    /// Completes the task automatically when disposed if not already completed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is not null && CompletionState == CompletionState.InProgress)
        {
            await Reporter.CompleteTaskAsync(this, CompletionState.Completed, null, CancellationToken.None).ConfigureAwait(false);
        }
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
    Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default);

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
                IsComplete = false,
                IsError = false,
                IsWarning = false,
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

        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText);
        task.Reporter = this;
        task.ParentStep = parentStep;
        
        // Add task to parent step's task list
        lock (parentStep)
        {
            parentStep.TasksList.Add(task);
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = statusText,
                IsComplete = false,
                IsError = false,
                IsWarning = false,
                StepId = step.Id
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        return task;
    }

    public async Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        bool shouldSendUpdate;
        lock (step)
        {
            // If already complete, this is a no-op (idempotent)
            if (step.CompletionState != CompletionState.InProgress)
            {
                return;
            }

            step.CompletionState = completionState;
            step.CompletionText = completionText;
            shouldSendUpdate = true;
        }

        if (shouldSendUpdate)
        {
            var state = new PublishingActivity
            {
                Type = PublishingActivityTypes.Step,
                Data = new PublishingActivityData
                {
                    Id = step.Id,
                    StatusText = completionText,
                    IsComplete = true,
                    IsError = completionState == CompletionState.CompletedWithError,
                    IsWarning = completionState == CompletionState.CompletedWithWarning,
                    StepId = null
                }
            };

            await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);

            // Remove the completed step to prevent further updates.
            _steps.TryRemove(step.Id, out _);
        }
    }

    public async Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            // Parent step doesn't exist, this is a no-op (idempotent)
            return;
        }

        bool shouldSendUpdate;
        lock (parentStep)
        {
            // If parent step is complete, this is a no-op (idempotent)
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                return;
            }

            task.StatusText = statusText;
            shouldSendUpdate = true;
        }

        if (shouldSendUpdate)
        {
            var state = new PublishingActivity
            {
                Type = PublishingActivityTypes.Task,
                Data = new PublishingActivityData
                {
                    Id = task.Id,
                    StatusText = statusText,
                    IsComplete = false,
                    IsError = false,
                    IsWarning = false,
                    StepId = task.StepId
                }
            };

            await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            // Parent step doesn't exist, this is a no-op (idempotent)
            return;
        }

        // If task is already completed, this is a no-op (idempotent)
        if (task.CompletionState != CompletionState.InProgress)
        {
            return;
        }

        bool shouldSendUpdate;
        lock (parentStep)
        {
            // If parent step is complete, this is a no-op (idempotent)
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                return;
            }

            task.CompletionState = completionState;
            task.CompletionMessage = completionMessage ?? string.Empty;
            shouldSendUpdate = true;
        }

        if (shouldSendUpdate)
        {
            var state = new PublishingActivity
            {
                Type = PublishingActivityTypes.Task,
                Data = new PublishingActivityData
                {
                    Id = task.Id,
                    StatusText = task.StatusText,
                    IsComplete = true,
                    IsError = completionState == CompletionState.CompletedWithError,
                    IsWarning = completionState == CompletionState.CompletedWithWarning,
                    StepId = task.StepId,
                    CompletionMessage = completionMessage
                }
            };

            await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);

            // If task completed with error, mark parent step with warning state
            if (completionState == CompletionState.CompletedWithError && task.ParentStep is not null)
            {
                lock (parentStep)
                {
                    // Only update if parent step is still in progress
                    if (parentStep.CompletionState == CompletionState.InProgress)
                    {
                        // Mark the parent step for warning completion when disposed
                        parentStep.MarkForWarningCompletion();
                    }
                }
            }
        }
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
                IsComplete = true,
                IsError = !success,
                IsWarning = false
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    internal Channel<PublishingActivity> ActivityItemUpdated { get; } = Channel.CreateUnbounded<PublishingActivity>();
}
