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
        var step = await reporter.CreateStepAsync("1", "step initial", default);
        await reporter.CompleteStepAsync(step, "step completed", default);

        Assert.NotNull(step);
        Assert.Equal("1", step.Id);
        Assert.True(step.IsComplete);
    }

    [Fact]
    public async Task CanCreateTask()
    {
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = new PublishingStep("step-1", "step initial");
        var task = await reporter.CreateTaskAsync(step, "step-1", "task initial", default);
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, "task completed", default);

        Assert.NotNull(task);
        Assert.Equal("task-1", task.Id);
        Assert.Equal("step-1", task.StepId);
    }
}
