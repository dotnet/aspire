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
        var stepId = "test-step";
        var title = "Test Step";

        // Act
        var step = await reporter.CreateStepAsync(stepId, title, CancellationToken.None);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(stepId, step.Id);
        Assert.Equal(title, step.Title);
        Assert.False(step.IsComplete);
        Assert.Equal(string.Empty, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepId, activity.Data.Id);
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
        var stepId = "test-step";
        var taskId = "test-task";
        var statusText = "Test Task";

        // Create parent step first
        await reporter.CreateStepAsync(stepId, "Parent Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        var task = await reporter.CreateTaskAsync(taskId, stepId, statusText, CancellationToken.None);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(taskId, task.Id);
        Assert.Equal(stepId, task.StepId);
        Assert.Equal(statusText, task.StatusText);
        Assert.Equal(TaskCompletionState.InProgress, task.CompletionState);
        Assert.Equal(string.Empty, task.CompletionMessage);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Task, activity.Type);
        Assert.Equal(taskId, activity.Data.Id);
        Assert.Equal(statusText, activity.Data.StatusText);
        Assert.Equal(stepId, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var nonExistentStepId = "non-existent-step";
        var taskId = "test-task";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(taskId, nonExistentStepId, "Test Task", CancellationToken.None));

        Assert.Contains($"Step with ID '{nonExistentStepId}' does not exist.", exception.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        // Create and complete step
        var step = await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CancellationToken.None);

        // Act & Assert - Step is now removed from dictionary when completed
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None));

        Assert.Contains($"Step with ID '{stepId}' does not exist.", exception.Message);
    }

    [Fact]
    public async Task CompleteStepAsync_CompletesStepAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var completionText = "Step completed successfully";

        var step = await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);

        // Clear the step creation activity
        reporter.ActivityItemUpdated.Reader.TryRead(out _);

        // Act
        await reporter.CompleteStepAsync(step, completionText, CancellationToken.None);

        // Assert
        Assert.True(step.IsComplete);
        Assert.Equal(completionText, step.CompletionText);

        // Verify activity was emitted
        var activityReader = reporter.ActivityItemUpdated.Reader;
        Assert.True(activityReader.TryRead(out var activity));
        Assert.Equal(PublishingActivityTypes.Step, activity.Type);
        Assert.Equal(stepId, activity.Data.Id);
        Assert.Equal(completionText, activity.Data.StatusText);
        Assert.True(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesTaskAndEmitsActivity()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";
        var newStatusText = "Updated status";

        await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Initial status", CancellationToken.None);

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
        Assert.Equal(taskId, activity.Data.Id);
        Assert.Equal(newStatusText, activity.Data.StatusText);
        Assert.Equal(stepId, activity.Data.StepId);
        Assert.False(activity.Data.IsComplete);
    }

    [Fact]
    public async Task UpdateTaskAsync_ThrowsWhenParentStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Initial status", CancellationToken.None);

        // Simulate step removal by creating a task with invalid step ID
        task = new PublishingTask(taskId, "non-existent-step", "Initial status");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));

        Assert.Contains("Parent step with ID 'non-existent-step' does not exist.", exception.Message);
    }

    [Fact]
    public async Task UpdateTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        var step = await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Initial status", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CancellationToken.None);

        // Act & Assert - Step is now removed from dictionary when completed
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));

        Assert.Contains($"Parent step with ID '{stepId}' does not exist.", exception.Message);
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
        var stepId = "test-step";
        var taskId = "test-task";
        var completionMessage = "Task completed";

        await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None);

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
        Assert.Equal(taskId, activity.Data.Id);
        Assert.Equal(stepId, activity.Data.StepId);
        Assert.True(activity.Data.IsComplete);
        Assert.Equal(expectedIsError, activity.Data.IsError);
        Assert.Equal(expectedIsWarning, activity.Data.IsWarning);
        Assert.Equal(completionMessage, activity.Data.CompletionMessage);
    }

    [Fact]
    public async Task CompleteTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        var step = await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CancellationToken.None);

        // Act & Assert - Step is now removed from dictionary when completed
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None));

        Assert.Contains($"Parent step with ID '{stepId}' does not exist.", exception.Message);
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
                var stepId = $"step-{i}";
                var step = await reporter.CreateStepAsync(stepId, $"Step {i}", CancellationToken.None);

                // Create tasks for each step concurrently
                var taskCreationTasks = Enumerable.Range(0, tasksPerStep)
                    .Select(async j =>
                    {
                        var taskId = $"task-{i}-{j}";
                        var task = await reporter.CreateTaskAsync(taskId, stepId, $"Task {i}-{j}", CancellationToken.None);
                        await reporter.UpdateTaskAsync(task, $"Updated Task {i}-{j}", CancellationToken.None);
                        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);
                        return task;
                    });

                var completedTasks = await Task.WhenAll(taskCreationTasks);
                await reporter.CompleteStepAsync(step, $"Step {i} completed", CancellationToken.None);

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
        var stepId = "test-step";
        var taskId = "test-task";

        await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None);

        // Act
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, task.CompletionMessage);
    }

    [Fact]
    public async Task CreateStepAsync_WithSameId_ThrowsException()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "duplicate-step";

        // Act
        await reporter.CreateStepAsync(stepId, "First Step", CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateStepAsync(stepId, "Second Step", CancellationToken.None));

        Assert.Contains($"Step with ID '{stepId}' already exists.", exception.Message);
    }

    [Fact]
    public async Task CompleteTaskAsync_ThrowsWhenTaskAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None);

        // Complete the task first time
        await reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None);

        // Act & Assert - Try to complete the same task again
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None));

        Assert.Contains($"Cannot complete task '{taskId}' with state 'Completed'. Only 'InProgress' tasks can be completed.", exception.Message);
    }

    [Fact]
    public async Task CompleteStepAsync_RemovesStepFromDictionary()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter();
        var stepId = "test-step";
        var taskId = "test-task";

        var step = await reporter.CreateStepAsync(stepId, "Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(taskId, stepId, "Test Task", CancellationToken.None);

        // Act - Complete the step
        await reporter.CompleteStepAsync(step, "Step completed", CancellationToken.None);

        // Assert - Verify that operations on tasks belonging to the completed step now fail
        // because the step has been removed from the dictionary
        var updateException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));
        Assert.Contains($"Parent step with ID '{stepId}' does not exist.", updateException.Message);

        var completeException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, TaskCompletionState.Completed, null, CancellationToken.None));
        Assert.Contains($"Parent step with ID '{stepId}' does not exist.", completeException.Message);

        // Also verify that creating new tasks for the completed step fails
        var createException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync("new-task", stepId, "New Task", CancellationToken.None));
        Assert.Contains($"Step with ID '{stepId}' does not exist.", createException.Message);
    }
}
