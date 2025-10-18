// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// A no-op implementation of <see cref="IPipelineActivityReporter"/> for testing purposes.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class NullPublishingActivityReporter : IPipelineActivityReporter
{
    /// <inheritdoc />
    public Task<IPublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IPublishingStep>(new NullPublishingStep());
    }

    /// <inheritdoc />
    public Task CompletePublishAsync(string? completionMessage = null, CompletionState? completionState = null, bool isDeploy = false, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class NullPublishingStep : IPublishingStep
{
    public Task<IPublishingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IPublishingTask>(new NullPublishingTask());
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

[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class NullPublishingTask : IPublishingTask
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
