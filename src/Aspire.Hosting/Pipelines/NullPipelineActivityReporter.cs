// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// A no-op implementation of <see cref="IPipelineActivityReporter"/> for testing purposes.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class NullPublishingActivityReporter : IPipelineActivityReporter
{
    /// <inheritdoc />
    public Task<IReportingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReportingStep>(new NullPublishingStep());
    }

    /// <inheritdoc />
    public Task CompletePublishAsync(string? completionMessage = null, CompletionState? completionState = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class NullPublishingStep : IReportingStep
{
    public Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReportingTask>(new NullPublishingTask());
    }

    public void Log(LogLevel logLevel, string message, bool enableMarkdown)
    {
        // No-op for null implementation
    }

    public Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class NullPublishingTask : IReportingTask
{
    public Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
