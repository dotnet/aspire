// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Minimalistic reporter that does nothing.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class NullPublishingActivityProgressReporter : IPublishingActivityProgressReporter
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullPublishingActivityProgressReporter"/>.
    /// </summary>
    public static NullPublishingActivityProgressReporter Instance { get; } = new NullPublishingActivityProgressReporter();

    private NullPublishingActivityProgressReporter()
    {
    }

    /// <inheritdoc/>
    public Task<PublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken)
    {
        var step = new PublishingStep(Guid.NewGuid().ToString(), title)
        {
            Reporter = this
        };
        return Task.FromResult(step);
    }

    /// <inheritdoc/>
    public Task<PublishingTask> CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken)
    {
        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText, step)
        {
            Reporter = this
        };
        step.AddTask(task);
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken = default)
    {
        step.CompletionState = completionState;
        step.CompletionText = completionText;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken)
    {
        task.StatusText = statusText;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        task.CompletionState = completionState;
        task.CompletionMessage = completionMessage ?? string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CompletePublishAsync(CompletionState? completionState = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CreatePromptAsync(string displayText, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CompletePromptAsync(string promptId, string? response, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
