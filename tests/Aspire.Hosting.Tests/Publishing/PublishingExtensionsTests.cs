// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class PublishingExtensionsTests
{
    [Fact]
    public async Task PublishingStepExtensions_UpdateStatus_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var result = await step.UpdateStatusAsync("Updated status", CancellationToken.None);

        // Assert
        Assert.Equal(step, result);
        Assert.Equal("Updated status", step.StatusText);
    }

    [Fact]
    public async Task PublishingStepExtensions_Succeed_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var result = await step.SucceedAsync("Success message", CancellationToken.None);

        // Assert
        Assert.Equal(step, result);
        Assert.True(step.IsComplete);
        Assert.Equal("Success message", step.CompletionText);
    }

    [Fact]
    public async Task PublishingTaskExtensions_UpdateStatus_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.UpdateStatusAsync("Updated status", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal("Updated status", task.StatusText);
    }

    [Fact]
    public async Task PublishingTaskExtensions_Succeed_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.SucceedAsync("Success message", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal(TaskCompletionState.Completed, task.CompletionState);
        Assert.Equal("Success message", task.CompletionMessage);
    }

    [Fact]
    public async Task PublishingTaskExtensions_Warn_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.WarnAsync("Warning message", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal(TaskCompletionState.CompletedWithWarning, task.CompletionState);
        Assert.Equal("Warning message", task.CompletionMessage);
    }

    [Fact]
    public async Task PublishingTaskExtensions_Fail_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.FailAsync("Error message", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal(TaskCompletionState.CompletedWithError, task.CompletionState);
        Assert.Equal("Error message", task.CompletionMessage);
    }
}