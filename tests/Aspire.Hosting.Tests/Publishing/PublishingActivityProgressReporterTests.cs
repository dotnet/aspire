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
        Assert.Equal(CompletionState.InProgress, step.CompletionState);
        Assert.Equal(string.Empty, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
        Assert.Equal(title, activity.Data.StatusText);
        Assert.Equal(CompletionState.InProgress, activity.Data.CompletionState);
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
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
        Assert.Equal(string.Empty, task.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.Equal(statusText, activity.Data.StatusText);
        Assert.Equal(step.Id, activity.Data.StepId);
        Assert.Equal(CompletionState.InProgress, activity.Data.CompletionState);
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
        await reporter.CompleteStepAsync(step, completionText, isError ? CompletionState.CompletedWithError : CompletionState.Completed, CancellationToken.None);

        // Assert
        Assert.Equal(expectedIsError ? CompletionState.CompletedWithError : CompletionState.Completed, step.CompletionState);
        Assert.Equal(completionText, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
        Assert.Equal(completionText, activity.Data.StatusText);
        Assert.Equal(expectedIsError ? CompletionState.CompletedWithError : CompletionState.Completed, activity.Data.CompletionState);
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
        Assert.Equal(CompletionState.InProgress, activity.Data.CompletionState);
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
    [InlineData(CompletionState.Completed)]
    [InlineData(CompletionState.CompletedWithWarning)]
    [InlineData(CompletionState.CompletedWithError)]
    public async Task CompleteTaskAsync_CompletesTaskWithCorrectStateAndEmitsActivity(
        CompletionState completionState)
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
        Assert.Equal(completionState, activity.Data.CompletionState);
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
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Assert - Task should still be in progress since complete was a no-op
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
    }

    [Theory]
    [InlineData(CompletionState.Completed, "Publishing completed successfully")]
    [InlineData(CompletionState.CompletedWithWarning, "Publishing completed with warnings")]
    [InlineData(CompletionState.CompletedWithError, "Publishing completed with errors")]
    public async Task CompletePublishAsync_EmitsCorrectActivity(CompletionState completionState, string expectedStatusText)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        // Act
        await reporter.CompletePublishAsync(completionState, CancellationToken.None);

        // Assert
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Type);
        Assert.Equal(PublishingActivityTypes.PublishComplete, activity.Data.Id);
        Assert.Equal(expectedStatusText, activity.Data.StatusText);
        Assert.Equal(completionState, activity.Data.CompletionState);
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
                        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);
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
            Assert.NotEqual(CompletionState.InProgress, result.Step.CompletionState);
            Assert.Equal(tasksPerStep, result.Tasks.Length);

            foreach (var task in result.Tasks)
            {
                Assert.Equal(CompletionState.Completed, task.CompletionState);
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
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

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
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Act - Try to complete the same task again - should be a no-op
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Assert - Task should still be completed
        Assert.Equal(CompletionState.Completed, task.CompletionState);
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
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);
        // Task should still be InProgress since complete was a no-op
        Assert.Equal(CompletionState.InProgress, task.CompletionState);

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
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
        Assert.Equal("Test Step", step.CompletionText);

        // Verify completion activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(step.Id, activity.Data.Id);
        Assert.Equal("Test Step", activity.Data.StatusText);
        Assert.Equal(CompletionState.Completed, activity.Data.CompletionState);
    }

    [Fact]
    public async Task PublishingStep_DisposeAsync_DoesNothingWhenAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Complete the step explicitly first
        await reporter.CompleteStepAsync(step, "Explicit completion", CompletionState.Completed, CancellationToken.None);

        // Clear activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the step after explicit completion
        await step.DisposeAsync();

        // Assert - No additional activity should be emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.False(activityReader.TryRead(out _));

        // Step should retain its explicit completion text
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
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
        Assert.Equal(CompletionState.Completed, task.CompletionState);
        Assert.Equal(string.Empty, task.CompletionMessage);

        // Verify completion activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(task.Id, activity.Data.Id);
        Assert.Equal(CompletionState.Completed, activity.Data.CompletionState);
    }

    [Fact]
    public async Task PublishingTask_Dispose_DoesNothingWhenAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the task explicitly first
        await reporter.CompleteTaskAsync(task, CompletionState.CompletedWithWarning, "Explicit completion", CancellationToken.None);

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
        Assert.Equal(CompletionState.CompletedWithWarning, task.CompletionState);
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
        await reporter.CompleteStepAsync(step, "Step completed", CompletionState.Completed, CancellationToken.None);

        // Act - Dispose the task after parent step is removed - should not throw
        await task.DisposeAsync();

        // Assert - Task should still be in progress since parent step was removed
        Assert.Equal(CompletionState.InProgress, task.CompletionState);
    }

    [Fact]
    public async Task DisposalPattern_UsageExample()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();

        // Create step and tasks
        var step = await reporter.CreateStepAsync("Publish Artifacts", CancellationToken.None);
        var pkgTask = await reporter.CreateTaskAsync(step, "Zipping assets", CancellationToken.None);
        var pushTask = await reporter.CreateTaskAsync(step, "Pushing to registry", CancellationToken.None);

        // Simulate some work
        await reporter.UpdateTaskAsync(pkgTask, "50% complete", CancellationToken.None);
        await reporter.UpdateTaskAsync(pushTask, "Uploading...", CancellationToken.None);

        // Verify state before disposal
        Assert.Equal(CompletionState.InProgress, pkgTask.CompletionState);
        Assert.Equal(CompletionState.InProgress, pushTask.CompletionState);
        Assert.Equal(CompletionState.InProgress, step.CompletionState);

        // Act - Manually dispose to test automatic completion
        await pushTask.DisposeAsync();
        await pkgTask.DisposeAsync();
        await step.DisposeAsync();

        // Assert - All should be completed after disposal
        Assert.Equal(CompletionState.Completed, pkgTask.CompletionState);
        Assert.Equal(CompletionState.Completed, pushTask.CompletionState);
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
    }

    [Fact]
    public async Task DisposalPattern_AwaitUsing_WorksCorrectly()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        PublishingStep step;
        PublishingTask pkgTask;
        PublishingTask pushTask;

        // Act - Use the actual await using pattern as intended
        {
            await using var disposableStep = await reporter.CreateStepAsync("Publish Artifacts", CancellationToken.None);
            step = disposableStep;

            {
                await using var disposablePkgTask = await reporter.CreateTaskAsync(step, "Zipping assets", CancellationToken.None);
                pkgTask = disposablePkgTask;
                
                await using var disposablePushTask = await reporter.CreateTaskAsync(step, "Pushing to registry", CancellationToken.None);
                pushTask = disposablePushTask;

                // Simulate some work
                await reporter.UpdateTaskAsync(pkgTask, "50% complete", CancellationToken.None);
                await reporter.UpdateTaskAsync(pushTask, "Uploading...", CancellationToken.None);

                // Verify state before disposal
                Assert.Equal(CompletionState.InProgress, pkgTask.CompletionState);
                Assert.Equal(CompletionState.InProgress, pushTask.CompletionState);
            }
            
            // Tasks should be completed after their await using scope ends
            Assert.Equal(CompletionState.Completed, pkgTask.CompletionState);
            Assert.Equal(CompletionState.Completed, pushTask.CompletionState);
            Assert.Equal(CompletionState.InProgress, step.CompletionState); // Step should still be in progress
        }

        // Step should be completed after its await using scope ends
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
    }

    [Fact]
    public async Task PublishingStep_DisposeAsync_CompletesAllChildTasks()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task1 = await reporter.CreateTaskAsync(step, "Task 1", CancellationToken.None);
        var task2 = await reporter.CreateTaskAsync(step, "Task 2", CancellationToken.None);

        // Verify initial state
        Assert.Equal(CompletionState.InProgress, task1.CompletionState);
        Assert.Equal(CompletionState.InProgress, task2.CompletionState);
        Assert.Equal(CompletionState.InProgress, step.CompletionState);

        // Act - Dispose the step without completing the tasks
        await step.DisposeAsync();

        // Assert - All tasks and the step should be completed
        Assert.Equal(CompletionState.Completed, task1.CompletionState);
        Assert.Equal(CompletionState.Completed, task2.CompletionState);
        Assert.NotEqual(CompletionState.InProgress, step.CompletionState);
    }

    [Fact]
    public async Task PublishingTask_CompleteWithError_SetsParentStepToWarning()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Act - Complete the task with error
        await reporter.CompleteTaskAsync(task, CompletionState.CompletedWithError, "Error message", CancellationToken.None);

        // Assert - Task should be completed with error and parent step should have warning state for disposal
        Assert.Equal(CompletionState.CompletedWithError, task.CompletionState);
        Assert.Equal("Error message", task.CompletionMessage);

        // Dispose the step to see the warning state
        await step.DisposeAsync();
        Assert.Equal(CompletionState.CompletedWithWarning, step.CompletionState);
    }

    [Fact]
    public async Task PublishingStep_HasChildTasksProperty()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Initially no tasks
        Assert.Empty(step.Tasks);

        // Add tasks
        var task1 = await reporter.CreateTaskAsync(step, "Task 1", CancellationToken.None);
        var task2 = await reporter.CreateTaskAsync(step, "Task 2", CancellationToken.None);

        // Verify tasks are in the collection
        Assert.Equal(2, step.Tasks.Count);
        Assert.Contains(task1, step.Tasks);
        Assert.Contains(task2, step.Tasks);
    }

    [Fact]
    public async Task PublishingTask_HasParentStepProperty()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Assert - Task should have reference to parent step
        Assert.Equal(step, task.ParentStep);
        Assert.Equal(step.Id, task.StepId);
    }

    [Fact]
    public async Task GetAggregatedCompletionState_ReturnsWorstStateFromTasks()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task1 = await reporter.CreateTaskAsync(step, "Task 1", CancellationToken.None);
        var task2 = await reporter.CreateTaskAsync(step, "Task 2", CancellationToken.None);
        var task3 = await reporter.CreateTaskAsync(step, "Task 3", CancellationToken.None);

        // Complete tasks with different states
        await reporter.CompleteTaskAsync(task1, CompletionState.Completed, null, CancellationToken.None);
        await reporter.CompleteTaskAsync(task2, CompletionState.CompletedWithWarning, null, CancellationToken.None);
        await reporter.CompleteTaskAsync(task3, CompletionState.CompletedWithError, null, CancellationToken.None);

        // Act
        var aggregatedState = step.GetAggregatedCompletionState();

        // Assert - Should return the worst state (CompletedWithError)
        Assert.Equal(CompletionState.CompletedWithError, aggregatedState);
    }

    [Fact]
    public async Task GetAggregatedCompletionState_WithNoTasks_ReturnsStepState()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Act
        var aggregatedState = step.GetAggregatedCompletionState();

        // Assert - Should return the step's own state since there are no tasks
        Assert.Equal(CompletionState.InProgress, aggregatedState);
    }

    [Fact]
    public async Task StepDisposal_UsesAggregatedStateFromTasks()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task1 = await reporter.CreateTaskAsync(step, "Task 1", CancellationToken.None);
        _ = await reporter.CreateTaskAsync(step, "Task 2", CancellationToken.None); // task2 will be completed during disposal

        // Complete one task with warning
        await reporter.CompleteTaskAsync(task1, CompletionState.CompletedWithWarning, null, CancellationToken.None);
        // Leave task2 incomplete - it will be completed during disposal

        // Clear activities
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act - Dispose the step
        await step.DisposeAsync();

        // Assert - Step should be completed with warning state (worst of warning + completed)
        Assert.Equal(CompletionState.CompletedWithWarning, step.CompletionState);

        // Verify the step completion activity reflects the aggregated state
        var activityReader = reporter.ActivityItemUpdated.Reader;
        // Skip task completion activity for task2
        activityReader.TryRead(out _);
        // Get step completion activity
        Assert.True(activityReader.TryRead(out var stepActivity));
        Assert.Equal(PublishingActivityTypes.Step, stepActivity.Type);
        Assert.Equal(CompletionState.CompletedWithWarning, stepActivity.Data.CompletionState);
    }

    [Fact]
    public async Task PublishingActivityProgressReporter_GetAggregatedCompletionState_ReturnsWorstStateFromAllSteps()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var step1 = await reporter.CreateStepAsync("Step 1", CancellationToken.None);
        var step2 = await reporter.CreateStepAsync("Step 2", CancellationToken.None);
        var step3 = await reporter.CreateStepAsync("Step 3", CancellationToken.None);

        var task1 = await reporter.CreateTaskAsync(step1, "Task 1", CancellationToken.None);
        var task2 = await reporter.CreateTaskAsync(step2, "Task 2", CancellationToken.None);
        var task3 = await reporter.CreateTaskAsync(step3, "Task 3", CancellationToken.None);

        // Complete tasks with different states
        await reporter.CompleteTaskAsync(task1, CompletionState.Completed, null, CancellationToken.None);
        await reporter.CompleteTaskAsync(task2, CompletionState.CompletedWithWarning, null, CancellationToken.None);
        await reporter.CompleteTaskAsync(task3, CompletionState.CompletedWithError, null, CancellationToken.None);

        // Complete steps
        await reporter.CompleteStepAsync(step1, "Step 1 completed", CompletionState.Completed, CancellationToken.None);
        await reporter.CompleteStepAsync(step2, "Step 2 completed", CompletionState.CompletedWithWarning, CancellationToken.None);
        await reporter.CompleteStepAsync(step3, "Step 3 completed", CompletionState.CompletedWithError, CancellationToken.None);

        // Act
        var aggregatedState = reporter.GetAggregatedCompletionState();

        // Assert - Should return the worst state (CompletedWithError)
        Assert.Equal(CompletionState.CompletedWithError, aggregatedState);
    }

    [Fact]
    public void CompletionStateExtensions_GetWorstState_ReturnsMaxValue()
    {
        // Test that the extension methods work correctly
        var states = new[] { CompletionState.InProgress, CompletionState.Completed, CompletionState.CompletedWithWarning, CompletionState.CompletedWithError };
        var worstState = CompletionStateExtensions.GetWorstState(states);
        Assert.Equal(CompletionState.CompletedWithError, worstState);

        // Test two-parameter version
        var worst = CompletionStateExtensions.GetWorstState(CompletionState.Completed, CompletionState.CompletedWithWarning);
        Assert.Equal(CompletionState.CompletedWithWarning, worst);
    }
}
