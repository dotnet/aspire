// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class NullPublishingActivityProgressReporterTests
{
    [Fact]
    public async Task CanUseNullReporter()
    {
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("step initial", default);
        await reporter.CompleteStepAsync(step, "step completed", default);

        Assert.NotNull(step);
        Assert.True(step.CompletionState != CompletionState.InProgress);
    }

    [Fact]
    public async Task CanCreateTask()
    {
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = new PublishingStep("step-1", "step initial");
        var task = await reporter.CreateTaskAsync(step, "task initial", default);
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, "task completed", default);

        Assert.NotNull(task);
        Assert.NotNull(task.Id);
        Assert.NotEmpty(task.Id);
        Assert.Equal(step.Id, task.StepId);
    }
}
