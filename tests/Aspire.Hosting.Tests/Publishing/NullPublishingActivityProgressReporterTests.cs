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
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
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

    [Fact]
    public async Task NullReporter_SupportsDisposal_ForStep()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        
        // Create step and assign to variable outside the scope
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        
        // Verify initial state
        Assert.Equal(CompletionState.InProgress, step.CompletionState);
        
        // Manually dispose to test the disposal behavior
        await step.DisposeAsync();
        
        // Assert - Step should be completed after disposal
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
        Assert.Equal("Test Step", step.CompletionText);
    }

    [Fact]
    public async Task NullReporter_SupportsDisposal_ForTask()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        
        // Create task and verify initial state
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
        
        // Manually dispose to test the disposal behavior
        await task.DisposeAsync();

        // Assert - Task should be completed after disposal
        Assert.Equal(CompletionState.Completed, task.CompletionState);
    }

    [Fact]
    public async Task NullReporter_StepCompletesChildTasksOnDisposal()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task1 = await reporter.CreateTaskAsync(step, "Task 1", CancellationToken.None);
        var task2 = await reporter.CreateTaskAsync(step, "Task 2", CancellationToken.None);

        // Verify initial state
        Assert.Equal(CompletionState.InProgress, task1.CompletionState);
        Assert.Equal(CompletionState.InProgress, task2.CompletionState);
        Assert.Equal(CompletionState.InProgress, step.CompletionState);

        // Act - Dispose the step
        await step.DisposeAsync();

        // Assert - All tasks and the step should be completed
        Assert.Equal(CompletionState.Completed, task1.CompletionState);
        Assert.Equal(CompletionState.Completed, task2.CompletionState);
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
    }

    [Fact]
    public async Task NullReporter_TaskErrorSetsParentStepToWarning()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Act - Complete the task with error
        await reporter.CompleteTaskAsync(task, CompletionState.CompletedWithError, "Error message", CancellationToken.None);

        // Assert - Task should be completed with error
        Assert.Equal(CompletionState.CompletedWithError, task.CompletionState);
        Assert.Equal("Error message", task.CompletionMessage);

        // Dispose the step to see the warning state
        await step.DisposeAsync();
        Assert.Equal(CompletionState.CompletedWithWarning, step.CompletionState);
    }
}
