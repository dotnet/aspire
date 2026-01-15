// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents a publishing step, which can contain multiple tasks.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IReportingStep : IAsyncDisposable
{
    /// <summary>
    /// Creates a new task within this step.
    /// </summary>
    /// <param name="statusText">The initial status text for the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created task.</returns>
    Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a message at the specified level within this step.
    /// </summary>
    /// <param name="logLevel">The log level for the message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="enableMarkdown">Whether to enable Markdown formatting for the message.</param>
    void Log(LogLevel logLevel, string message, bool enableMarkdown);

    /// <summary>
    /// Completes the step with the specified completion text and state.
    /// </summary>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="completionState">The completion state for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default);
}
