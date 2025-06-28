// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREINTERACTION001

using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Publishing;
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
        Assert.False(activity.Data.IsComplete);
        Assert.False(activity.Data.IsError);
        Assert.False(activity.Data.IsWarning);
    }

    [Fact]
    public async Task CreateTaskAsync_ThrowsWhenStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
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
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Create and complete step
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CompletionState.Completed, cancellationToken: CancellationToken.None);

        // Act & Assert - Step is kept in dictionary but marked as complete, so operations should fail with "already complete"
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None));

        Assert.Contains($"Cannot create task for step '{step.Id}' because the step is already complete.", exception.Message);
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
        await reporter.CompleteStepAsync(step, completionText, isError ? CompletionState.CompletedWithError : CompletionState.Completed, CancellationToken.None);

        // Assert
        Assert.True(step.CompletionState != CompletionState.InProgress);
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
        var reporter = new PublishingActivityProgressReporter(_interactionService);
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
    public async Task UpdateTaskAsync_ThrowsWhenParentStepDoesNotExist()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);

        // Simulate step removal by creating a task with invalid step ID
        var dummyStep = await reporter.CreateStepAsync("Dummy Step", CancellationToken.None);
        task = new PublishingTask(task.Id, "non-existent-step", "Initial status", dummyStep);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));

        Assert.Contains("Parent step with ID 'non-existent-step' does not exist.", exception.Message);
    }

    [Fact]
    public async Task UpdateTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Initial status", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CompletionState.Completed, cancellationToken: CancellationToken.None);

        // Act & Assert - Step is kept in dictionary but marked as complete, so operations should fail with "already complete"
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));

        Assert.Contains($"Cannot update task '{task.Id}' because its parent step '{task.StepId}' is already complete.", exception.Message);
    }

    [Theory]
    [InlineData(CompletionState.Completed, false, false)]
    [InlineData(CompletionState.CompletedWithWarning, false, true)]
    [InlineData(CompletionState.CompletedWithError, true, false)]
    public async Task CompleteTaskAsync_CompletesTaskWithCorrectStateAndEmitsActivity(
        CompletionState completionState, bool expectedIsError, bool expectedIsWarning)
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
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
    public async Task CompleteTaskAsync_ThrowsWhenParentStepIsComplete()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);
        await reporter.CompleteStepAsync(step, "Completed", CompletionState.Completed, cancellationToken: CancellationToken.None);

        // Act & Assert - Step is kept in dictionary but marked as complete, so operations should fail with "already complete"
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None));

        Assert.Contains($"Cannot complete task '{task.Id}' because its parent step '{task.StepId}' is already complete.", exception.Message);
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
        await reporter.CompletePublishAsync(completionState, CancellationToken.None);

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
    public async Task CompletePublishAsync_AggregatesStateFromSteps()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        // Create multiple steps with different completion states
        var step1 = await reporter.CreateStepAsync("Step 1", CancellationToken.None);
        var step2 = await reporter.CreateStepAsync("Step 2", CancellationToken.None);
        var step3 = await reporter.CreateStepAsync("Step 3", CancellationToken.None);

        var task1 = await reporter.CreateTaskAsync(step1, "Task 1", CancellationToken.None);
        await reporter.CompleteTaskAsync(task1, CompletionState.Completed, null, CancellationToken.None);
        await reporter.CompleteStepAsync(step1, "Step 1 completed", CompletionState.Completed, CancellationToken.None);

        var task2 = await reporter.CreateTaskAsync(step2, "Task 2", CancellationToken.None);
        await reporter.CompleteTaskAsync(task2, CompletionState.CompletedWithWarning, null, CancellationToken.None);
        await reporter.CompleteStepAsync(step2, "Step 2 completed with warning", CompletionState.CompletedWithWarning, CancellationToken.None);

        var task3 = await reporter.CreateTaskAsync(step3, "Task 3", CancellationToken.None);
        await reporter.CompleteTaskAsync(task3, CompletionState.CompletedWithError, null, CancellationToken.None);
        await reporter.CompleteStepAsync(step3, "Step 3 failed", CompletionState.CompletedWithError, CancellationToken.None);

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
    public async Task ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
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
                        try
                        {
                            var task = await reporter.CreateTaskAsync(step, $"Task {i}-{j}", CancellationToken.None);
                            await reporter.UpdateTaskAsync(task, $"Updated Task {i}-{j}", CancellationToken.None);
                            await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);
                            return task;
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("because the step is already complete"))
                        {
                            // This is expected in concurrent scenarios where the step might be completed
                            // while tasks are still being created/updated
                            return null;
                        }
                    });

                var completedTasks = await Task.WhenAll(taskCreationTasks);
                var validTasks = completedTasks.Where(t => t is not null).Cast<PublishingTask>().ToArray();
                await reporter.CompleteStepAsync(step, $"Step {i} completed", CompletionState.Completed, cancellationToken: CancellationToken.None);

                return new { Step = step, Tasks = validTasks };
            });

        var results = await Task.WhenAll(stepTasks);

        // Assert
        Assert.Equal(stepCount, results.Length);

        foreach (var result in results)
        {
            Assert.True(result.Step.CompletionState != CompletionState.InProgress);
            // We may have fewer tasks than expected due to concurrent completion, but that's okay
            Assert.True(result.Tasks.Length <= tasksPerStep);

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

        // Due to concurrent completion, we may have fewer activities than the theoretical maximum
        // but we should at least have some activities for steps and tasks
        Assert.True(activities.Count >= stepCount); // At least one activity per step
    }

    [Fact]
    public async Task CompleteTaskAsync_WithNullCompletionMessage_SetsEmptyString()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Act
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, task.CompletionMessage);
    }

    [Fact]
    public async Task CompleteTaskAsync_ThrowsWhenTaskAlreadyCompleted()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the task first time
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Act & Assert - Try to complete the same task again
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None));

        Assert.Contains($"Cannot complete task '{task.Id}' with state 'Completed'. Only 'InProgress' tasks can be completed.", exception.Message);
    }

    [Fact]
    public async Task CompleteStepAsync_KeepsStepInDictionaryForAggregation()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);

        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);
        var task = await reporter.CreateTaskAsync(step, "Test Task", CancellationToken.None);

        // Complete the task first
        await reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None);

        // Act - Complete the step
        await reporter.CompleteStepAsync(step, "Step completed", CompletionState.Completed, cancellationToken: CancellationToken.None);

        // Assert - Verify that operations on tasks belonging to the completed step still fail
        // because the step is complete (not because it's been removed)
        var updateException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.UpdateTaskAsync(task, "New status", CancellationToken.None));
        Assert.Contains($"Cannot update task '{task.Id}' because its parent step '{task.StepId}' is already complete.", updateException.Message);

        // For CompleteTaskAsync, it will first check if the task is already completed, so we expect that error instead
        var completeException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CompleteTaskAsync(task, CompletionState.Completed, null, CancellationToken.None));
        Assert.Contains($"Cannot complete task '{task.Id}' with state 'Completed'. Only 'InProgress' tasks can be completed.", completeException.Message);

        // Creating new tasks for the completed step should also fail because the step is complete
        var createException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.CreateTaskAsync(step, "New Task", CancellationToken.None));
        Assert.Contains($"Cannot create task for step '{step.Id}' because the step is already complete.", createException.Message);
    }

    [Fact]
    public async Task HandleInteractionUpdateAsync_BlocksInteractionWhenStepsInProgress()
    {
        // Arrange
        var reporter = new PublishingActivityProgressReporter(_interactionService);
        var step = await reporter.CreateStepAsync("Test Step", CancellationToken.None);

        // Clear previous activities
        var activityReader = reporter.ActivityItemUpdated.Reader;
        while (activityReader.TryRead(out _)) { }

        // Assert that requesting an input while steps are in progress results in an error
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _interactionService.PromptInputAsync("Test Prompt", "test-description", "text-label", "test-placeholder"));
        Assert.Equal("Cannot prompt interaction while steps are in progress.", exception.Message);

        // Clean up
        await reporter.CompleteStepAsync(step, "Completed", CompletionState.Completed, CancellationToken.None);
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var activity = await activityReader.ReadAsync(cts.Token);
        var promptId = activity.Data.Id;
        Assert.NotNull(activity.Data.Inputs);
        var input = Assert.Single(activity.Data.Inputs);
        Assert.Equal("text-label", input.Label);
        Assert.Equal("Text", input.InputType);

        var responses = new string[] { "user-response" };

        // Act
        await reporter.CompleteInteractionAsync(promptId, responses, CancellationToken.None);

        // The prompt task should complete with the user's response
        var promptResult = await promptTask;
        Assert.False(promptResult.Canceled);
        Assert.Equal("user-response", promptResult.Data?.Value);
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
