// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREINTERACTION001

using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Publishing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class PublishingActivityProgressReporterTests
{
    private readonly InteractionService _interactionService = CreateInteractionService();

    [Fact]
    public async Task CreateStepAsync_CreatesStepAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var title = "Test Step";

        // Act
        var step = await reporter.CreateStepAsync(title, CancellationToken.None);

        // Assert
        Assert.NotNull(step);
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.NotNull(stepInternal.Id);
        Assert.NotEmpty(stepInternal.Id);
        Assert.Equal(title, stepInternal.Title);
        Assert.Equal(CompletionState.InProgress, stepInternal.CompletionState);
        Assert.Equal(string.Empty, stepInternal.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepInternal.Id, activity.Data.Id);
        Assert.Equal(title, activity.Data.StatusText);
        Assert.False(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
        Assert.Null(activity.Data.StepId);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesTaskAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var statusText = "Test Task";

        // Create parent step first
        var step = await reporter.CreateStepAsync("Parent Step", CancellationToken.None);
        var stepInternal = Assert.IsType<PublishingStep>(step);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        var task = await step.CreateTaskAsync(statusText, CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.NotNull(taskInternal.Id);
        Assert.NotEmpty(taskInternal.Id);
        Assert.Equal(stepInternal.Id, taskInternal.StepId);
        Assert.Equal(statusText, taskInternal.StatusText);
        Assert.Equal(CompletionState.InProgress, taskInternal.CompletionState);
        Assert.Equal(string.Empty, taskInternal.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskInternal.Id, activity.Data.Id);
        Assert.Equal(statusText, activity.Data.StatusText);
        Assert.Equal(stepInternal.Id, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var nonExistentStep = new PublishingStep(reporter, "non-existent-step", "Non-existent Step");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nonExistentStep.CreateTaskAsync("Test Task", CancellationToken.None));

        Assert.Contains("Step with ID 'non-existent-step' does not exist", exception.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Create and complete step
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        await step.CompleteAsync("Completed", CompletionState.Completed, CancellationToken.None);

        // Act & Assert - Step is completed, so creating tasks should fail
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => step.CreateTaskAsync("Test Task", CancellationToken.None));

        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Contains($"Cannot create task for step '{stepInternal.Id}' because the step is already complete.", exception.Message);
    }

    [Theory]
    [InlineData(false, "Step completed successfully", false)]
    [InlineData(true, "Step completed with errors", true)]
    public async Task CompleteStepAsync_CompletesStepWithCorrectErrorStateAndEmitsActivity(bool isError, string completionText, bool expectedIsError)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await step.CompleteAsync(completionText, isError ? CompletionState.CompletedWithError : CompletionState.Completed, CancellationToken.None);

        // Assert
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.NotEqual(CompletionState.InProgress, stepInternal.CompletionState);
        Assert.Equal(completionText, stepInternal.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepInternal.Id, activity.Data.Id);
        Assert.Equal(completionText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesTaskAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var newStatusText = "Updated status";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Initial status", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await task.UpdateAsync(newStatusText, CancellationToken.None);

        // Assert
        var taskInternal = Assert.IsType<PublishingTask>(task);
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(newStatusText, taskInternal.StatusText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskInternal.Id, activity.Data.Id);
        Assert.Equal(newStatusText, activity.Data.StatusText);
        Assert.Equal(stepInternal.Id, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
    }

    [Fact]
    public async Task UpdateTaskAsync_ThrowsWhenParentStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Initial status", CancellationToken.None);

        // Simulate step removal by creating a task with invalid step ID
        var dummyStep = await reporter.CreateStepAsync("Dummy Step", CancellationToken.None);
        var dummyStepInternal = Assert.IsType<PublishingStep>(dummyStep);
        var taskInternal = Assert.IsType<PublishingTask>(task);
        var invalidTask = new PublishingTask(taskInternal.Id, "non-existent-step", "Initial status", dummyStepInternal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => invalidTask.UpdateAsync("New status", CancellationToken.None));

        Assert.Contains("Parent step with ID 'non-existent-step' does not exist", exception.Message);
    }

    [Fact]
    public async Task UpdateTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Initial status", CancellationToken.None);
        await step.CompleteAsync("Completed", CompletionState.Completed, CancellationToken.None);

        // Act & Assert - Step is completed, so updating tasks should fail
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => task.UpdateAsync("New status", CancellationToken.None));

        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.Contains($"Cannot update task '{taskInternal.Id}' because its parent step", exception.Message);
    }

    [Fact]
    public async Task CompleteTaskAsync_CompletesTaskWithCorrectStateAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var completionMessage = "Task completed";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Use the public API which only supports successful completion
        await task.CompleteAsync(completionMessage, cancellationToken: CancellationToken.None);

        // Assert
        var taskInternal = Assert.IsType<PublishingTask>(task);
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.Completed, taskInternal.CompletionState);
        Assert.Equal(completionMessage, taskInternal.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskInternal.Id, activity.Data.Id);
        Assert.Equal(stepInternal.Id, activity.Data.StepId);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(completionMessage, activity.Data.CompletionMessage);

        // Note: The public API only supports successful completion, so we can't test different completion states
        // through the public API. These would need to be tested through internal APIs if needed.
    }

    [Fact]
    public async Task CompleteTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);
        await step.CompleteAsync("Completed", CompletionState.Completed, CancellationToken.None);

        // Act & Assert - Step is completed, so completing tasks should fail
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => task.CompleteAsync(null, cancellationToken: CancellationToken.None));

        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.Contains($"Cannot complete task '{taskInternal.Id}' because its parent step", exception.Message);
    }

    [Theory]
    [InlineData(CompletionState.Completed, "Publishing completed successfully", false)]
    [InlineData(CompletionState.CompletedWithError, "Publishing completed with errors", true)]
    [InlineData(CompletionState.CompletedWithWarning, "Publishing completed with warnings", false)]
    public async Task CompletePublishAsync_EmitsCorrectActivity(CompletionState completionState, string expectedStatusText, bool expectedIsError)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Act
        await reporter.CompletePublishAsync(null, completionState, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Type);
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Data.Id);
        Assert.Equal(expectedStatusText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.Equal(completionState == CompletionState.CompletedWithWarning, activity.Data.IsWarning);
    }

    [Fact]
    public async Task CompletePublishAsync_EmitsCorrectActivity_WithCompletionMessage()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var expectedStatusText = "Some error occurred";

        // Act
        await reporter.CompletePublishAsync(expectedStatusText, CompletionState.CompletedWithError, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Type);
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Data.Id);
        Assert.Equal(expectedStatusText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.True(activity.Data.IsError);
    }

    [Fact]
    public async Task CompletePublishAsync_AggregatesStateFromSteps()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Create multiple steps with different completion states
        var step1 = await reporter.CreateStepAsync("Step 1", CancellationToken.None);
        var step2 = await reporter.CreateStepAsync("Step 2", CancellationToken.None);
        var step3 = await reporter.CreateStepAsync("Step 3", CancellationToken.None);

        var task1 = await step1.CreateTaskAsync("Task 1", CancellationToken.None);
        await task1.CompleteAsync(null, cancellationToken: CancellationToken.None);
        await step1.CompleteAsync("Step 1 completed", CompletionState.Completed, CancellationToken.None);

        var task2 = await step2.CreateTaskAsync("Task 2", CancellationToken.None);
        await task2.CompleteAsync(null, cancellationToken: CancellationToken.None);
        await step2.CompleteAsync("Step 2 completed with warning", CompletionState.CompletedWithWarning, CancellationToken.None);

        var task3 = await step3.CreateTaskAsync("Task 3", CancellationToken.None);
        await task3.CompleteAsync(null, cancellationToken: CancellationToken.None);
        await step3.CompleteAsync("Step 3 failed", CompletionState.CompletedWithError, CancellationToken.None);

        // Clear previous activities
        var activityReader = reporter.ActivityItemUpdated.Reader;
        while (activityReader.TryRead(out _)) { }

        // Act - Complete publish without specifying state (should aggregate)
        await reporter.CompletePublishAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Type);
        Assert.Equal("Publishing completed with errors", activity.Data.StatusText);
        Assert.True(activity.Data.IsError); // Should be error because step3 had an error (highest severity)
        Assert.True(activity.Data.IsComplete);
    }

    [Fact]
    public async Task CompleteTaskAsync_WithNullCompletionMessage_SetsEmptyString()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Act
        await task.CompleteAsync(null, cancellationToken: CancellationToken.None);

        // Assert
        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.Equal(string.Empty, taskInternal.CompletionMessage);
    }

    [Fact]
    public async Task CompleteTaskAsync_ThrowsWhenTaskAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Complete the task first time
        await task.CompleteAsync(null, cancellationToken: CancellationToken.None);

        // Act & Assert - Try to complete the same task again
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => task.CompleteAsync(null, cancellationToken: CancellationToken.None));

        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.Contains($"Cannot complete task '{taskInternal.Id}' with state 'Completed'. Only 'InProgress' tasks can be completed.", exception.Message);
    }

    [Fact]
    public async Task CompleteStepAsync_ThrowsWhenStepAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Complete the step first time
        await step.CompleteAsync("Complete", cancellationToken: CancellationToken.None);

        // Act & Assert - Try to complete the same step again
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => step.CompleteAsync("Complete again", cancellationToken: CancellationToken.None));

        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Contains($"Cannot complete step '{stepInternal.Id}' with state 'Completed'. Only 'InProgress' steps can be completed.", exception.Message);
    }

    [Fact]
    public async Task CompleteStepAsync_KeepsStepInDictionaryForAggregation()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Complete the task first
        await task.CompleteAsync(null, cancellationToken: CancellationToken.None);

        // Act - Complete the step
        await step.CompleteAsync("Step completed", CompletionState.Completed, CancellationToken.None);

        // Assert - Verify that operations on tasks belonging to the completed step still fail
        // because the step is complete (not because it's been removed)
        var updateException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => task.UpdateAsync("New status", CancellationToken.None));
        var taskInternal = Assert.IsType<PublishingTask>(task);
        Assert.Contains($"Cannot update task '{taskInternal.Id}' because its parent step", updateException.Message);

        // For CompleteTaskAsync, it will first check if the task is already completed, so we expect that error instead
        var completeException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => task.CompleteAsync(null, cancellationToken: CancellationToken.None));
        Assert.Contains($"Cannot complete task '{taskInternal.Id}' with state 'Completed'. Only 'InProgress' tasks can be completed.", completeException.Message);

        // Creating new tasks for the completed step should also fail because the step is complete
        var createException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => step.CreateTaskAsync("New Task", CancellationToken.None));
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Contains($"Cannot create task for step '{stepInternal.Id}' because the step is already complete.", createException.Message);
    }

    [Fact]
    public async Task HandleInteractionUpdateAsync_BlocksInteractionWhenStepsInProgress()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        await using var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear previous activities
        var activityReader = reporter.ActivityItemUpdated.Reader;
        while (activityReader.TryRead(out _)) { }

        // Assert that requesting an input while steps are in progress results in an error
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _interactionService.PromptInputAsync("Test Prompt", "test-description", "text-label", "test-placeholder"));
        Assert.Equal("Cannot prompt interaction while steps are in progress.", exception.Message);
    }

    [Fact]
    public async Task CompleteInteractionAsync_ProcessesUserResponsesCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Start a prompt interaction
        var promptTask = _interactionService.PromptInputAsync("Test Prompt", "test-description", "text-label", "test-placeholder");

        // Get the interaction ID from the activity that was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var activity = await activityReader.ReadAsync().DefaultTimeout();
        var promptId = activity.Data.Id;
        Assert.NotNull(activity.Data.Inputs);
        var input = Assert.Single(activity.Data.Inputs);
        Assert.Equal("text-label", input.Label);
        Assert.Equal("Text", input.InputType);

        var responses = new string[] { "user-response" };

        // Act
        await reporter.CompleteInteractionAsync(promptId, responses, CancellationToken.None).DefaultTimeout();

        // The prompt task should complete with the user's response
        var promptResult = await promptTask.DefaultTimeout();
        Assert.False(promptResult.Canceled);
        Assert.Equal("user-response", promptResult.Data?.Value);
    }

    [Fact]
    public async Task CalculateAggregatedState_WithNoTasks_ReturnsCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var stepInternal = Assert.IsType<PublishingStep>(step);
        var aggregatedState = stepInternal.CalculateAggregatedState();

        // Assert
        Assert.Equal(CompletionState.Completed, aggregatedState);
    }

    [Fact]
    public async Task DisposeAsync_StepWithNoTasks_CompletesWithSuccessState()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await step.DisposeAsync();

        // Assert
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.Completed, stepInternal.CompletionState);

        // Verify activity was emitted for step completion
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepInternal.Id, activity.Data.Id);
        Assert.Equal(CompletionStates.Completed, activity.Data.CompletionState);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task DisposeAsync_StepWithCompletedTasks_CompletesWithSuccessState()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task1 = await step.CreateTaskAsync("Task 1", CancellationToken.None);
        var task2 = await step.CreateTaskAsync("Task 2", CancellationToken.None);

        // Complete all tasks successfully
        await task1.SucceedAsync(null, CancellationToken.None);
        await task2.SucceedAsync(null, CancellationToken.None);

        // Clear previous activities
        while (reporter.ActivityItemUpdated.Reader.TryRead(out _)) { }

        // Act
        await step.DisposeAsync();

        // Assert
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.Completed, stepInternal.CompletionState);

        // Verify activity was emitted for step completion
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepInternal.Id, activity.Data.Id);
        Assert.Equal(CompletionStates.Completed, activity.Data.CompletionState);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task DisposeAsync_StepAlreadyCompleted_DoesNotCompleteAgain()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Complete the step explicitly first
        await step.CompleteAsync("Step completed manually", CompletionState.Completed, CancellationToken.None);

        // Clear activities
        while (reporter.ActivityItemUpdated.Reader.TryRead(out _)) { }

        // Act - Dispose should not cause another completion
        await step.DisposeAsync();

        // Assert - No new activities should be emitted
        Assert.False(reporter.ActivityItemUpdated.Reader.TryRead(out _));
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.Completed, stepInternal.CompletionState);
        Assert.Equal("Step completed manually", stepInternal.CompletionText);
    }

    [Fact]
    public async Task CompleteWithWarningAsync_CompletesTaskWithWarningAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var completionMessage = "Task completed with warning";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        var taskInternal = Assert.IsType<PublishingTask>(task);
        await taskInternal.WarnAsync(completionMessage, CancellationToken.None);

        // Assert
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.CompletedWithWarning, taskInternal.CompletionState);
        Assert.Equal(completionMessage, taskInternal.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskInternal.Id, activity.Data.Id);
        Assert.Equal(stepInternal.Id, activity.Data.StepId);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.True(activity.Data.IsWarning);
        Assert.Equal(completionMessage, activity.Data.CompletionMessage);
    }

    [Fact]
    public async Task FailAsync_CompletesTaskWithErrorAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var completionMessage = "Task failed with error";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await step.CreateTaskAsync("Test Task", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        var taskInternal = Assert.IsType<PublishingTask>(task);
        await taskInternal.FailAsync(completionMessage, CancellationToken.None);

        // Assert
        var stepInternal = Assert.IsType<PublishingStep>(step);
        Assert.Equal(CompletionState.CompletedWithError, taskInternal.CompletionState);
        Assert.Equal(completionMessage, taskInternal.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskInternal.Id, activity.Data.Id);
        Assert.Equal(stepInternal.Id, activity.Data.StepId);
        Assert.True(activity.Data.IsComplete);
        Assert.True(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
        Assert.Equal(completionMessage, activity.Data.CompletionMessage);
    }

    internal static InteractionService CreateInteractionService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<InteractionService>();
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<InteractionService>>();
        return new InteractionService(logger, new DistributedApplicationOptions(), provider);
    }
}
