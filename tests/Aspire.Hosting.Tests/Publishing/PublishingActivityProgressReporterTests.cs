// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Publishing;

public class PublishingActivityProgressReporterTests
{
    [Fact]
    public async Task CreateStepAsync_CreatesStepAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var title = "Test Step";

        // Act
        var step = await reporter.CreateStepAsync(title, CancellationToken.None);

        // Assert
        Assert.NotNull(step);
        Assert.NotNull(step.Id);
        Assert.NotEmpty(step.Id);
        Assert.Equal(title, step.Title);
        Assert.False(step.IsComplete);
        Assert.False(step.IsError);
        Assert.Equal(string.Empty, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
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
        var reporter = new PublishingActivityProgressReporter();
        var statusText = "Test Task";

        // Create parent step first
        var step = await reporter.CreateStepAsync("Parent Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        var task = await reporter.CreateTaskAsync(step, statusText, CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        Assert.NotNull(task.Id);
        Assert.NotEmpty(task.Id);
        Assert.Equal(step.Id, task.StepId);
        Assert.Equal(statusText, task.StatusText);
        Assert.Equal(TaskCompletionState.InProgress, task.CompletionState);
        Assert.Equal(string.Empty, task.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.Equal(statusText, activity.Data.StatusText);
        Assert.Equal(step.Id, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var nonExistentStep = new PublishingStep("non-existent-step", "Non-existent Step");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(nonExistentStep, "Test Task", CancellationToken.None));

        Assert.Contains($"Step with ID 'non-existent-step' does not exist.", exception.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        // Create and complete step
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", cancellationToken: CancellationToken.None);

        // Act & Assert - Step is now removed from dictionary when completed
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None));

        Assert.Contains($"Step with ID '{step.Id}' does not exist.", exception.Message);
    }

    [Theory]
    [InlineData(false, "Step completed successfully", false)]
    [InlineData(true, "Step completed with errors", true)]
    public async Task CompleteStepAsync_CompletesStepWithCorrectErrorStateAndEmitsActivity(bool isError, string completionText, bool expectedIsError)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await reporter.CompleteStepAsync(step, completionText, isError, CancellationToken.None);

        // Assert
        Assert.True(step.IsComplete);
        Assert.Equal(expectedIsError, step.IsError);
        Assert.Equal(completionText, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
        Assert.Equal(completionText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesTaskAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var newStatusText = "Updated status";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await reporter.UpdateTaskAsync(task, newStatusText, CancellationToken.None);

        // Assert
        Assert.Equal(newStatusText, task.StatusText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.Equal(newStatusText, activity.Data.StatusText);
        Assert.Equal(step.Id, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
    }

    [Fact]
    public async Task UpdateTaskAsync_IsIdempotentWhenParentStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Simulate step removal by creating a task with invalid step ID
        task = new PublishingTask(task.Id, "non-existent-step", "Initial status");

        // Act - UpdateTaskAsync should be a no-op when parent step doesn't exist
        await reporter.UpdateTaskAsync(task, "New status", CancellationToken.None);

        // Assert - Task status should not have changed since update was a no-op
        Assert.Equal("Initial status", task.StatusText);
    }

    [Fact]
    public async Task UpdateTaskAsync_IsIdempotentWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", cancellationToken: CancellationToken.None);

        // Act - UpdateTaskAsync should be a no-op when parent step is complete
        await reporter.UpdateTaskAsync(task, "New status", CancellationToken.None);

        // Assert - Task status should not have changed since update was a no-op
        Assert.Equal("Initial status", task.StatusText);
    }

    [Theory]
    [InlineData(TaskCompletionState.Completed, false, false)]
    [InlineData(TaskCompletionState.CompletedWithWarning, false, true)]
    [InlineData(TaskCompletionState.CompletedWithError, true, false)]
    public async Task CompleteTaskAsync_CompletesTaskWithCorrectStateAndEmitsActivity(
        TaskCompletionState completionState, bool expectedIsError, bool expectedIsWarning)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var completionMessage = "Task completed";

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await reporter.CompleteTaskAsync(task, completionState, completionMessage, CancellationToken.None);

        // Assert
        Assert.Equal(completionState, task.CompletionState);
        Assert.Equal(completionMessage, task.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.Equal(step.Id, activity.Data.StepId);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.Equal(expectedIsWarning, activity.Data.IsWarning);
        Assert.Equal(completionMessage, activity.Data.CompletionMessage);
    }

    [Fact]
    public async Task CompleteTaskAsync_IsIdempotentWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", cancellationToken: CancellationToken.None);

        // Act - CompleteTaskAsync should be a no-op when parent step is complete
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Assert - Task should still be in progress since complete was a no-op
        Assert.Equal(TaskCompletionState.InProgress, task.CompletionState);
    }

    [Theory]
    [InlineData(true, "Publishing completed successfully", false)]
    [InlineData(false, "Publishing completed with errors", true)]
    public async Task CompletePublishAsync_EmitsCorrectActivity(bool success, string expectedStatusText, bool expectedIsError)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        // Act
        await reporter.CompletePublishAsync(success, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Type);
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Data.Id);
        Assert.Equal(expectedStatusText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        const int stepCount = 10;
        const int tasksPerStep = 5;

        // Act - Create steps and tasks concurrently
        var stepTasks = Enumerable.Range(0, stepCount)
            .Select(async i =>
            {
                var step = await reporter.CreateStepAsync($"Step {i}", CancellationToken.None);

                // Create tasks for each step concurrently
                var taskCreationTasks = Enumerable.Range(0, tasksPerStep)
                    .Select(async j =>
                    {
                        var task = await reporter.CreateTaskAsync(step, $"Task {i}-{j}", CancellationToken.None);
                        await reporter.UpdateTaskAsync(task, $"Updated Task {i}-{j}", CancellationToken.None);
                        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);
                        return task;
                    });

                var completedTasks = await Task.WhenAll(taskCreationTasks);
                await reporter.CompleteStepAsync(step, $"Step {i} completed", cancellationToken: CancellationToken.None);

                return new { Step = step, Tasks = completedTasks };
            });

        var results = await Task.WhenAll(stepTasks);

        // Assert
        Assert.Equal(stepCount, results.Length);

        foreach (var result in results)
        {
            Assert.True(result.Step.IsComplete);
            Assert.Equal(tasksPerStep, result.Tasks.Length);

            foreach (var task in result.Tasks)
            {
                Assert.Equal(TaskCompletionState.Completed, task.CompletionState);
            }
        }

        // Verify all activities were emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        var activities = new List<PublishingActivity>();

        while (activityReader.TryRead(out var activity))
        {
            activities.Add(activity);
        }

        // Expected activities: stepCount steps + (stepCount * tasksPerStep) tasks * 3 operations + stepCount step completions
        var expectedActivityCount = stepCount + (stepCount * tasksPerStep * 3) + stepCount;
        Assert.True(activities.Count >= expectedActivityCount);
    }

    [Fact]
    public async Task CompleteTaskAsync_WithNullCompletionMessage_SetsEmptyString()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Act
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, task.CompletionMessage);
    }

    [Fact]
    public async Task CompleteTaskAsync_IsIdempotentWhenTaskAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the task first time
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Act - Try to complete the same task again - should be a no-op
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Assert - Task should still be completed
        Assert.Equal(TaskCompletionState.Completed, task.CompletionState);
    }

    [Fact]
    public async Task CompleteStepAsync_RemovesStepFromDictionary()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Act - Complete the step
        await reporter.CompleteStepAsync(step, "Step completed", cancellationToken: CancellationToken.None);

        // Assert - Verify that operations on tasks belonging to the completed step are now no-ops
        // because the step has been removed from the dictionary

        // UpdateTaskAsync should be a no-op
        await reporter.UpdateTaskAsync(task, "New status", CancellationToken.None);
        // Task status should not have changed since update was a no-op
        Assert.NotEqual("New status", task.StatusText);

        // CompleteTaskAsync should be a no-op  
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);
        // Task should still be InProgress since complete was a no-op
        Assert.Equal(TaskCompletionState.InProgress, task.CompletionState);

        // Creating new tasks for the completed step still fails (this is expected behavior)
        var createException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(step, "New Task", CancellationToken.None));
        Assert.Contains($"Step with ID '{step.Id}' does not exist.", createException.Message);
    }

    [Fact]
    public async Task PublishingStep_DisposeAsync_CompletesStepAutomatically()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the step without explicit completion
        await step.DisposeAsync();

        // Assert - Verify step is completed
        Assert.True(step.IsComplete);
        Assert.Equal("Test Step", step.CompletionText);

        // Verify completion activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
        Assert.Equal("Test Step", activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
    }

    [Fact]
    public async Task PublishingStep_DisposeAsync_DoesNothingWhenAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Complete the step explicitly first
        await reporter.CompleteStepAsync(step, "Explicit completion", isError: false, CancellationToken.None);

        // Clear activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the step after explicit completion
        await step.DisposeAsync();

        // Assert - No additional activity should be emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.False(activityReader.TryRead(out _));

        // Step should retain its explicit completion text
        Assert.True(step.IsComplete);
        Assert.Equal("Explicit completion", step.CompletionText);
    }

    [Fact]
    public async Task PublishingTask_Dispose_CompletesTaskAutomatically()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Clear previous activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the task without explicit completion
        await task.DisposeAsync();

        // Assert - Verify task is completed
        Assert.Equal(TaskCompletionState.Completed, task.CompletionState);
        Assert.Equal(string.Empty, task.CompletionMessage);

        // Verify completion activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task PublishingTask_Dispose_DoesNothingWhenAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the task explicitly first
        await reporter.CompleteTaskAsync(task, TaskCompletionState.CompletedWithWarning, "Explicit completion", CancellationToken.None);

        // Clear activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the task after explicit completion
        await task.DisposeAsync();

        // Assert - No additional activity should be emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.False(activityReader.TryRead(out _));

        // Task should retain its explicit completion state
        Assert.Equal(TaskCompletionState.CompletedWithWarning, task.CompletionState);
        Assert.Equal("Explicit completion", task.CompletionMessage);
    }

    [Fact]
    public async Task PublishingTask_Dispose_HandlesParentStepRemoved()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the step first, which removes it from the dictionary
        await reporter.CompleteStepAsync(step, "Step completed", isError: false, CancellationToken.None);

        // Act - Dispose the task after parent step is removed - should not throw
        await task.DisposeAsync();

        // Assert - Task should still be in progress since parent step was removed
        Assert.Equal(TaskCompletionState.InProgress, task.CompletionState);
    }

    [Fact]
    public async Task DisposalPattern_UsageExample()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        // Act - Use the disposal pattern as shown in the issue example
        await using var step = await reporter.CreateStepAsync("Publish Artifacts", CancellationToken.None);

        await using var pkgTask = await reporter.CreateTaskAsync(step, "Zipping assets", CancellationToken.None);
        await using var pushTask = await reporter.CreateTaskAsync(step, "Pushing to registry", CancellationToken.None);

        // Simulate some work
        await reporter.UpdateTaskAsync(pkgTask, "50% complete", CancellationToken.None);
        await reporter.UpdateTaskAsync(pushTask, "Uploading...", CancellationToken.None);

        // Tasks and step will be automatically completed when disposed

        // Assert - All should be completed after using blocks
        Assert.Equal(TaskCompletionState.Completed, pkgTask.CompletionState);
        Assert.Equal(TaskCompletionState.Completed, pushTask.CompletionState);
        Assert.True(step.IsComplete);
    }

    [Fact]
    public async Task PublishingStep_DisposeAsync_RespectsIsErrorProperty()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Complete the step with error state first, then dispose should respect that
        await reporter.CompleteStepAsync(step, "Completed with error", isError: true, CancellationToken.None);

        // Clear activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the step, which should be a no-op since already completed
        await step.DisposeAsync();

        // Assert - Step should retain its error state
        Assert.True(step.IsComplete);
        Assert.True(step.IsError);
        Assert.Equal("Completed with error", step.CompletionText);

        // No additional activity should be emitted since already completed
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.False(activityReader.TryRead(out _));
    }
}
