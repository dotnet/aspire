// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Extension methods for <see cref="PublishingStep"/> and <see cref="PublishingTask"/> to provide direct operations.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class PublishingExtensions
{
    /// <summary>
    /// Creates a new publishing task parented to this step.
    /// </summary>
    /// <param name="step">The step to create a task for.</param>
    /// <param name="statusText">The initial status text for the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created publishing task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available or the step is already complete.</exception>
    public static async Task<PublishingTask> CreateTaskAsync(
        this PublishingStep step,
        string statusText,
        CancellationToken cancellationToken = default)
    {
        if (step.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this step.");
        }

        return await step.Reporter.CreateTaskAsync(step, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes a publishing step successfully.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingStep> SucceedAsync(
        this PublishingStep step,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (step.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this step.");
        }

        var completionText = message ?? "Completed successfully";
        await step.Reporter.CompleteStepAsync(step, completionText, isError: false, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Completes a publishing step with a warning.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingStep> WarnAsync(
        this PublishingStep step,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (step.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this step.");
        }

        var completionText = message ?? "Completed with warnings";
        await step.Reporter.CompleteStepAsync(step, completionText, isError: false, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Completes a publishing step with an error.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingStep> FailAsync(
        this PublishingStep step,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (step.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this step.");
        }

        var completionText = errorMessage ?? "Failed";
        await step.Reporter.CompleteStepAsync(step, completionText, isError: true, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Updates the status text of a publishing task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent step is complete or no reporter is available.</exception>
    public static async Task<PublishingTask> UpdateStatusAsync(
        this PublishingTask task,
        string statusText,
        CancellationToken cancellationToken = default)
    {
        if (task.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this task.");
        }

        await task.Reporter.UpdateTaskAsync(task, statusText, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Completes a publishing task successfully.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingTask> SucceedAsync(
        this PublishingTask task,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (task.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this task.");
        }

        await task.Reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, message, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Completes a publishing task with a warning.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingTask> WarnAsync(
        this PublishingTask task,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (task.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this task.");
        }

        await task.Reporter.CompleteTaskAsync(task, TaskCompletionState.CompletedWithWarning, message, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Completes a publishing task with an error.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reporter is available.</exception>
    public static async Task<PublishingTask> FailAsync(
        this PublishingTask task,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (task.Reporter is null)
        {
            throw new InvalidOperationException("No progress reporter is available for this task.");
        }

        await task.Reporter.CompleteTaskAsync(task, TaskCompletionState.CompletedWithError, errorMessage, cancellationToken).ConfigureAwait(false);
        return task;
    }
}