// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class PublishingTask : IPublishingTask
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
    public string Id { get; }

    /// <summary>
    /// The identifier of the step this task belongs to.
    /// </summary>
    public string StepId { get; }

    /// <summary>
    /// Reference to the parent step this task belongs to.
    /// </summary>
    public PublishingStep ParentStep { get; }

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
    /// Updates the status text of this task.
    /// </summary>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
    {
        await ParentStep.Reporter.UpdateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the task with the specified completion message.
    /// </summary>
    /// <param name="completionMessage">Optional completion message that will appear as a dimmed child message.</param>
    /// <param name="completionState">The completion state of the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        await ParentStep.Reporter.CompleteTaskAsync(this, completionState, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as completed with warnings.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task WarnAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        await ParentStep.Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithWarning, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as failed with an error.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task FailAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        await ParentStep.Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithError, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the task, completing it successfully if not already completed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (CompletionState != CompletionState.InProgress)
        {
            return;
        }

        // Auto-complete with success if not already completed
        await CompleteAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }
}
