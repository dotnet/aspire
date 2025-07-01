// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a publishing step, which can contain multiple tasks.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class PublishingStep : IPublishingStep
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
    /// The completion state of the step. Defaults to InProgress.
    /// The state is only aggregated from child tasks during disposal.
    /// </summary>
    public CompletionState CompletionState
    {
        get => _completionState;
        internal set => _completionState = value;
    }

    private CompletionState _completionState = CompletionState.InProgress;

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
    internal PublishingActivityProgressReporter? Reporter { get; set; }

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
            return CompletionState.Completed;
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
    public async Task<IPublishingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot create task: Reporter is not set.");
        }

        return await Reporter.CreateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the step with the specified completion text and state.
    /// </summary>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="completionState">The completion state for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete step: Reporter is not set.");
        }

        await Reporter.CompleteStepAsync(this, completionText, completionState, cancellationToken).ConfigureAwait(false);
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

        // Only complete the step if it's still in progress to avoid double completion
        if (CompletionState != CompletionState.InProgress)
        {
            return;
        }

        // Use the current completion state or calculate it from child tasks if still in progress
        var finalState = CalculateAggregatedState();

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
