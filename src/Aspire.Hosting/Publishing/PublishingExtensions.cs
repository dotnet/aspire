// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Extension methods for <see cref="IPublishingStep"/> and <see cref="IPublishingTask"/> to provide direct operations.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class PublishingExtensions
{
    /// <summary>
    /// Completes a publishing step successfully.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    public static async Task<IPublishingStep> SucceedAsync(
        this IPublishingStep step,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var completionText = message ?? "Completed successfully";
        await step.CompleteAsync(completionText, CompletionState.Completed, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Completes a publishing step with a warning.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    public static async Task<IPublishingStep> WarnAsync(
        this IPublishingStep step,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var completionText = message ?? "Completed with warnings";
        await step.CompleteAsync(completionText, CompletionState.CompletedWithWarning, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Completes a publishing step with an error.
    /// </summary>
    /// <param name="step">The step to complete.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed step.</returns>
    public static async Task<IPublishingStep> FailAsync(
        this IPublishingStep step,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var completionText = errorMessage ?? "Failed";
        await step.CompleteAsync(completionText, CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>
    /// Updates the status text of a publishing task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated task.</returns>
    public static async Task<IPublishingTask> UpdateStatusAsync(
        this IPublishingTask task,
        string statusText,
        CancellationToken cancellationToken = default)
    {
        await task.UpdateAsync(statusText, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Completes a publishing task successfully.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    public static async Task<IPublishingTask> SucceedAsync(
        this IPublishingTask task,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        await task.CompleteAsync(message, cancellationToken).ConfigureAwait(false);
        return task;
    }

    /// <summary>
    /// Completes a publishing task with a warning.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="message">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    public static async Task<IPublishingTask> WarnAsync(
        this IPublishingTask task,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (task is PublishingTask concreteTask)
        {
            await concreteTask.CompleteWithWarningAsync(message, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // For other implementations, fall back to normal completion
            await task.CompleteAsync(message, cancellationToken).ConfigureAwait(false);
        }
        return task;
    }

    /// <summary>
    /// Completes a publishing task with an error.
    /// </summary>
    /// <param name="task">The task to complete.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    public static async Task<IPublishingTask> FailAsync(
        this IPublishingTask task,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (task is PublishingTask concreteTask)
        {
            await concreteTask.FailAsync(errorMessage, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // For other implementations, fall back to normal completion
            await task.CompleteAsync(errorMessage, cancellationToken).ConfigureAwait(false);
        }
        return task;
    }
}