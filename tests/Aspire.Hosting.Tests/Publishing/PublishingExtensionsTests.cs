// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class PublishingExtensionsTests
{
    private readonly InteractionService _interactionService = PublishingActivityProgressReporterTests.CreateInteractionService();

    [Fact]
    public async Task PublishingStepExtensions_CreateTask_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var task = await step.CreateTaskAsync("Initial status", CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(step.Id, task.StepId);
        Assert.Equal("Initial status", task.StatusText);
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
    }

    [Fact]
    public async Task PublishingStepExtensions_CreateTask_ThrowsWhenNoReporter()
    {
        // Arrange
        var step = new PublishingStep("test-id", "Test Step");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => step.CreateTaskAsync("Initial status", CancellationToken.None));
        Assert.Equal("Cannot create task: Reporter is not set.", exception.Message);
    }

    [Fact]
    public async Task PublishingStepExtensions_CreateTask_WorksWithNullReporter()
    {
        // Arrange
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var task = await step.CreateTaskAsync("Initial status", CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(step.Id, task.StepId);
        Assert.Equal("Initial status", task.StatusText);
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
    }

    [Fact]
    public async Task PublishingStepExtensions_Succeed_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var result = await step.SucceedAsync("Success message", CancellationToken.None);

        // Assert
        Assert.Equal(step, result);
        Assert.True(step.CompletionState != CompletionState.InProgress);
        Assert.Equal("Success message", step.CompletionText);
    }

    [Fact]
    public async Task PublishingTaskExtensions_UpdateStatus_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
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
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.SucceedAsync("Success message", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal(CompletionState.Completed, task.CompletionState);
        Assert.Equal("Success message", task.CompletionMessage);
    }

    [Fact]
    public async Task PublishingTaskExtensions_Warn_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        var result = await task.WarnAsync("Warning message", CancellationToken.None);

        // Assert
        Assert.Equal(task, result);
        Assert.Equal(CompletionState.CompletedWithWarning, task.CompletionState);
        Assert.Equal("Warning message", task.CompletionMessage);
    }

    [Fact]
    public async Task PublishingTaskExtensions_Fail_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Act
        await task.FailAsync("Error message", CancellationToken.None);

        // Assert
        Assert.Equal(CompletionState.CompletedWithError, task.CompletionState);
        Assert.Equal("Error message", task.CompletionMessage);
    }
}
