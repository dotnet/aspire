// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Aspire.Hosting.Backchannel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the completion state of a publishing task.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum TaskCompletionState
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
/// <remarks>
/// Initializes a new instance of the <see cref="PublishingStep"/> class.
/// </remarks>
/// <param name="id">The unique identifier for the publishing step.</param>
/// <param name="title">The title of the publishing step.</param>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingStep(string id, string title)
{

    /// <summary>
    /// Unique Id of the step.
    /// </summary>
    public string Id { get; private set; } = id;

    /// <summary>
    /// The title of the publishing step.
    /// </summary>
    public string Title { get; private set; } = title;

    /// <summary>
    /// Indicates whether the step is complete.
    /// </summary>
    public bool IsComplete { get; internal set; }

    /// <summary>
    /// The completion text for the step.
    /// </summary>
    public string CompletionText { get; internal set; } = string.Empty;
}

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PublishingTask"/> class.
/// </remarks>
/// <param name="id">The unique identifier for the publishing task.</param>
/// <param name="stepId">The identifier of the step this task belongs to.</param>
/// <param name="statusText">The initial status text for the task.</param>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingTask(string id, string stepId, string statusText)
{
    /// <summary>
    /// Unique Id of the task.
    /// </summary>
    public string Id { get; private set; } = id;

    /// <summary>
    /// The identifier of the step this task belongs to.
    /// </summary>
    public string StepId { get; private set; } = stepId;

    /// <summary>
    /// The current status text of the task.
    /// </summary>
    public string StatusText { get; internal set; } = statusText;

    /// <summary>
    /// The completion state of the task.
    /// </summary>
    public TaskCompletionState CompletionState { get; internal set; } = TaskCompletionState.InProgress;

    /// <summary>
    /// Optional completion message for the task.
    /// </summary>
    public string CompletionMessage { get; internal set; } = string.Empty;
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
    /// Completes a publishing step with the specified completion text.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task CompleteStepAsync(PublishingStep step, string completionText, CancellationToken cancellationToken);

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
    Task CompleteTaskAsync(PublishingTask task, TaskCompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default);

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

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = title,
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
            if (parentStep.IsComplete)
            {
                throw new InvalidOperationException($"Cannot create task for step '{step.Id}' because the step is already complete.");
            }
        }

        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText);

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

    public async Task CompleteStepAsync(PublishingStep step, string completionText, CancellationToken cancellationToken)
    {
        lock (step)
        {
            step.IsComplete = true;
            step.CompletionText = completionText;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = completionText,
                IsComplete = true,
                IsError = false,
                IsWarning = false,
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
            if (parentStep.IsComplete)
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
                IsComplete = false,
                IsError = false,
                IsWarning = false,
                StepId = task.StepId
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteTaskAsync(PublishingTask task, TaskCompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            throw new InvalidOperationException($"Parent step with ID '{task.StepId}' does not exist.");
        }

        if (task.CompletionState != TaskCompletionState.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete task '{task.Id}' with state '{task.CompletionState}'. Only 'InProgress' tasks can be completed.");
        }

        lock (parentStep)
        {
            if (parentStep.IsComplete)
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
                IsComplete = true,
                IsError = completionState == TaskCompletionState.CompletedWithError,
                IsWarning = completionState == TaskCompletionState.CompletedWithWarning,
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
                IsComplete = true,
                IsError = !success,
                IsWarning = false
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    internal Channel<PublishingActivity> ActivityItemUpdated { get; } = Channel.CreateUnbounded<PublishingActivity>();
}
