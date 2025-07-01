// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingTask : IAsyncDisposable
{
    /// <summary>
    /// Updates the status text of this task.
    /// </summary>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(string statusText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the task with the specified completion message.
    /// </summary>
    /// <param name="completionMessage">Optional completion message that will appear as a dimmed child message.</param>
    /// <param name="completionState">The completion state of the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default);
}
