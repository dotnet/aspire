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
        Assert.True(step.IsComplete);
    }

    [Fact]
    public async Task CanCreateTask()
    {
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = new PublishingStep("step-1", "step initial");
        var task = await reporter.CreateTaskAsync(step, "task initial", default);
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, "task completed", default);

        Assert.NotNull(task);
        Assert.NotNull(task.Id);
        Assert.NotEmpty(task.Id);
        Assert.Equal(step.Id, task.StepId);
    }

    [Fact]
    public async Task NullReporter_SupportsDisposal_ForStep()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;

        // Act - Use disposal pattern with null reporter
        await using var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Assert - Step should be completed after disposal
        Assert.True(step.IsComplete);
        Assert.Equal("Test Step", step.CompletionText);
    }

    [Fact]
    public async Task NullReporter_SupportsDisposal_ForTask()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act - Use disposal pattern with null reporter
        using var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Assert - Task should be completed after disposal
        Assert.Equal(TaskCompletionState.Completed, task.CompletionState);
    }
}
