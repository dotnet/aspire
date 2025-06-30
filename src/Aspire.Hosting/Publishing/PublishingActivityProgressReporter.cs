// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREINTERACTION001

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Channels;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the completion state of a publishing activity (task, step, or top-level operation).
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum CompletionState
{
    /// <summary>
    /// The task is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// The task completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The task completed with warnings.
    /// </summary>
    CompletedWithWarning,

    /// <summary>
    /// The task completed with an error.
    /// </summary>
    CompletedWithError
}

/// <summary>
/// Internal interface for methods that step and task classes need to call on the reporter.
/// </summary>
internal interface IInternalPublishingActivityProgressReporter
{
    Task<PublishingTask> CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken);
    Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken);
    Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for reporting publishing activity progress.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingActivityProgressReporter
{
    /// <summary>
    /// Creates a new publishing step with the specified title.
    /// </summary>
    /// <param name="title">The title of the publishing step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The publishing step</returns>
    Task<IPublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that the entire publishing process has completed.
    /// </summary>
    /// <param name="completionState">The completion state of the publishing process. When null, the state is automatically aggregated from all steps.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompletePublishAsync(CompletionState? completionState = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a publishing step, which can contain multiple tasks.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingStep : IAsyncDisposable
{
    /// <summary>
    /// Creates a new task within this step.
    /// </summary>
    /// <param name="statusText">The initial status text for the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created task.</returns>
    Task<IPublishingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the step with the specified completion text and state.
    /// </summary>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="completionState">The completion state for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingTask : IAsyncDisposable
{
    /// <summary>
    /// Updates the status text of this task.
    /// </summary>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(string statusText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the task with the specified completion state and optional completion message.
    /// </summary>
    /// <param name="completionState">The completion state.</param>
    /// <param name="completionMessage">Optional completion message that will appear as a dimmed child message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(CompletionState completionState = CompletionState.Completed, string? completionMessage = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a publishing step, which can contain multiple tasks.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingStep : IPublishingStep
{
    private readonly ConcurrentDictionary<string, PublishingTask> _tasks = new();

    internal PublishingStep(string id, string title)
    {
        Id = id;
        Title = title;
    }

    /// <summary>
    /// Unique Id of the step.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// The title of the publishing step.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// The completion state of the step. Defaults to InProgress.
    /// The state is only aggregated from child tasks during disposal.
    /// </summary>
    public CompletionState CompletionState
    {
        get => _completionState;
        internal set => _completionState = value;
    }

    private CompletionState _completionState = CompletionState.InProgress;

    /// <summary>
    /// The completion text for the step.
    /// </summary>
    public string CompletionText { get; internal set; } = string.Empty;

    /// <summary>
    /// The collection of child tasks belonging to this step.
    /// </summary>
    public IReadOnlyDictionary<string, PublishingTask> Tasks => _tasks;

    /// <summary>
    /// The progress reporter that created this step.
    /// </summary>
    internal IInternalPublishingActivityProgressReporter? Reporter { get; set; }

    /// <summary>
    /// Adds a task to this step.
    /// </summary>
    internal void AddTask(PublishingTask task)
    {
        _tasks.TryAdd(task.Id, task);
    }

    /// <summary>
    /// Recalculates the completion state based on child tasks.
    /// </summary>
    internal CompletionState CalculateAggregatedState()
    {
        if (_tasks.IsEmpty)
        {
            return CompletionState.Completed;
        }

        var maxState = CompletionState.InProgress;
        foreach (var task in _tasks.Values)
        {
            if ((int)task.CompletionState > (int)maxState)
            {
                maxState = task.CompletionState;
            }
        }
        return maxState;
    }

    /// <summary>
    /// Creates a new task within this step.
    /// </summary>
    /// <param name="statusText">The initial status text for the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created task.</returns>
    public async Task<IPublishingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot create task: Reporter is not set.");
        }

        return await Reporter.CreateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the step with the specified completion text and state.
    /// </summary>
    /// <param name="completionText">The completion text for the step.</param>
    /// <param name="completionState">The completion state for the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete step: Reporter is not set.");
        }

        await Reporter.CompleteStepAsync(this, completionText, completionState, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the step and aggregates the final completion state from all child tasks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is null)
        {
            return;
        }

        // Only complete the step if it's still in progress to avoid double completion
        if (CompletionState != CompletionState.InProgress)
        {
            return;
        }

        // Use the current completion state or calculate it from child tasks if still in progress
        var finalState = CalculateAggregatedState();

        // Only set completion text if it has not been explicitly set
        var completionText = string.IsNullOrEmpty(CompletionText)
            ? finalState switch
            {
                CompletionState.Completed => $"{Title} completed successfully",
                CompletionState.CompletedWithWarning => $"{Title} completed with warnings",
                CompletionState.CompletedWithError => $"{Title} completed with errors",
                _ => $"{Title} completed"
            }
            : CompletionText;

        await Reporter.CompleteStepAsync(this, completionText, finalState, CancellationToken.None).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a publishing task, which belongs to a step.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingTask : IPublishingTask
{
    internal PublishingTask(string id, string stepId, string statusText, PublishingStep parentStep)
    {
        Id = id;
        StepId = stepId;
        StatusText = statusText;
        ParentStep = parentStep;
    }
    /// <summary>
    /// Unique Id of the task.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// The identifier of the step this task belongs to.
    /// </summary>
    public string StepId { get; private set; }

    /// <summary>
    /// Reference to the parent step this task belongs to.
    /// </summary>
    public PublishingStep ParentStep { get; internal set; }

    /// <summary>
    /// The current status text of the task.
    /// </summary>
    public string StatusText { get; internal set; }

    /// <summary>
    /// The completion state of the task.
    /// </summary>
    public CompletionState CompletionState { get; internal set; } = CompletionState.InProgress;

    /// <summary>
    /// Optional completion message for the task.
    /// </summary>
    public string CompletionMessage { get; internal set; } = string.Empty;

    /// <summary>
    /// The progress reporter that created this task.
    /// </summary>
    internal IInternalPublishingActivityProgressReporter? Reporter { get; set; }

    /// <summary>
    /// Updates the status text of this task.
    /// </summary>
    /// <param name="statusText">The new status text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot update task: Reporter is not set.");
        }

        await Reporter.UpdateTaskAsync(this, statusText, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the task with the specified completion state and optional completion message.
    /// </summary>
    /// <param name="completionState">The completion state.</param>
    /// <param name="completionMessage">Optional completion message that will appear as a dimmed child message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteAsync(CompletionState completionState = CompletionState.Completed, string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, completionState, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as completed with warnings.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task CompleteWithWarningAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot complete task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithWarning, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks the task as failed with an error.
    /// </summary>
    /// <param name="completionMessage">Optional completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task FailAsync(string? completionMessage = null, CancellationToken cancellationToken = default)
    {
        if (Reporter is null)
        {
            throw new InvalidOperationException("Cannot fail task: Reporter is not set.");
        }

        await Reporter.CompleteTaskAsync(this, CompletionState.CompletedWithError, completionMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the task, completing it successfully if not already completed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Reporter is null || CompletionState != CompletionState.InProgress)
        {
            return;
        }

        // Auto-complete with success if not already completed
        await CompleteAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }
}

internal sealed class PublishingActivityProgressReporter : IPublishingActivityProgressReporter, IInternalPublishingActivityProgressReporter, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, PublishingStep> _steps = new();
    private readonly InteractionService _interactionService;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _interactionServiceSubscriber;

    public PublishingActivityProgressReporter(InteractionService interactionService)
    {
        _interactionService = interactionService;
        _interactionServiceSubscriber = Task.Run(() => SubscribeToInteractionsAsync(_cancellationTokenSource.Token));
    }

    private static string ToBackchannelCompletionState(CompletionState state) => state switch
    {
        CompletionState.InProgress => CompletionStates.InProgress,
        CompletionState.Completed => CompletionStates.Completed,
        CompletionState.CompletedWithWarning => CompletionStates.CompletedWithWarning,
        CompletionState.CompletedWithError => CompletionStates.CompletedWithError,
        _ => CompletionStates.InProgress
    };

    public async Task<IPublishingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
    {
        var step = new PublishingStep(Guid.NewGuid().ToString(), title)
        {
            Reporter = this
        };
        _steps.TryAdd(step.Id, step);

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = step.Title,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = null
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        return step;
    }

    async Task<PublishingTask> IInternalPublishingActivityProgressReporter.CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(step.Id, out var parentStep))
        {
            throw new InvalidOperationException($"Step with ID '{step.Id}' does not exist.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot create task for step '{step.Id}' because the step is already complete.");
            }
        }

        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText, parentStep)
        {
            Reporter = this
        };

        // Add task to parent step
        parentStep.AddTask(task);

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = statusText,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = step.Id
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
        return task;
    }

    async Task IInternalPublishingActivityProgressReporter.CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken)
    {
        lock (step)
        {
            step.CompletionState = completionState;
            step.CompletionText = completionText;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Step,
            Data = new PublishingActivityData
            {
                Id = step.Id,
                StatusText = completionText,
                CompletionState = ToBackchannelCompletionState(completionState),
                StepId = null
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    async Task IInternalPublishingActivityProgressReporter.UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            throw new InvalidOperationException($"Parent step with ID '{task.StepId}' does not exist.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot update task '{task.Id}' because its parent step '{task.StepId}' is already complete.");
            }

            task.StatusText = statusText;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = statusText,
                CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                StepId = task.StepId
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    async Task IInternalPublishingActivityProgressReporter.CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage, CancellationToken cancellationToken)
    {
        if (!_steps.TryGetValue(task.StepId, out var parentStep))
        {
            throw new InvalidOperationException($"Parent step with ID '{task.StepId}' does not exist.");
        }

        if (task.CompletionState != CompletionState.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete task '{task.Id}' with state '{task.CompletionState}'. Only 'InProgress' tasks can be completed.");
        }

        lock (parentStep)
        {
            if (parentStep.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot complete task '{task.Id}' because its parent step '{task.StepId}' is already complete.");
            }

            task.CompletionState = completionState;
            task.CompletionMessage = completionMessage ?? string.Empty;
        }

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.Task,
            Data = new PublishingActivityData
            {
                Id = task.Id,
                StatusText = task.StatusText,
                CompletionState = ToBackchannelCompletionState(completionState),
                StepId = task.StepId,
                CompletionMessage = completionMessage
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompletePublishAsync(CompletionState? completionState = null, CancellationToken cancellationToken = default)
    {
        // Use provided state or aggregate from all steps
        var finalState = completionState ?? CalculateOverallAggregatedState();

        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.PublishComplete,
            Data = new PublishingActivityData
            {
                Id = PublishingActivityTypes.PublishComplete,
                StatusText = finalState switch
                {
                    CompletionState.Completed => "Publishing completed successfully",
                    CompletionState.CompletedWithWarning => "Publishing completed with warnings",
                    CompletionState.CompletedWithError => "Publishing completed with errors",
                    _ => "Publishing completed"
                },
                CompletionState = ToBackchannelCompletionState(finalState)
            }
        };

        await ActivityItemUpdated.Writer.WriteAsync(state, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Calculates the overall completion state by aggregating all steps.
    /// </summary>
    private CompletionState CalculateOverallAggregatedState()
    {
        if (_steps.IsEmpty)
        {
            return CompletionState.Completed;
        }

        var maxState = CompletionState.InProgress;
        foreach (var step in _steps.Values)
        {
            var stepState = step.CompletionState;
            if ((int)stepState > (int)maxState)
            {
                maxState = stepState;
            }
        }
        return maxState;
    }

    private async Task SubscribeToInteractionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var interaction in _interactionService.SubscribeInteractionUpdates(cancellationToken).ConfigureAwait(false))
            {
                await HandleInteractionUpdateAsync(interaction, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    /// <summary>
    /// Checks if there are any steps currently in progress.
    /// </summary>
    private bool HasStepsInProgress()
    {
        return _steps.Any(step => step.Value.CompletionState == CompletionState.InProgress);
    }

    private async Task HandleInteractionUpdateAsync(Interaction interaction, CancellationToken cancellationToken)
    {
        // Only handle input interaction types
        if (interaction.InteractionInfo is not Interaction.InputsInteractionInfo inputsInfo || inputsInfo.Inputs.Count == 0)
        {
            return;
        }

        if (interaction.State == Interaction.InteractionState.InProgress)
        {
            if (HasStepsInProgress())
            {
                await _interactionService.CompleteInteractionAsync(interaction.InteractionId, (interaction, ServiceProvider, cancellationToken) =>
                {
                    // Complete the interaction with an error state
                    interaction.CompletionTcs.TrySetException(new InvalidOperationException("Cannot prompt interaction while steps are in progress."));
                    return Task.FromResult(new InteractionCompletionState
                    {
                        Complete = false,
                        State = "Cannot prompt interaction while steps are in progress."
                    });
                }, cancellationToken).ConfigureAwait(false);
                return;
            }

            var promptInputs = inputsInfo.Inputs.Select(input => new PublishingPromptInput
            {
                Label = input.Label,
                InputType = input.InputType.ToString(),
                Required = input.Required,
                Options = input.Options,
                Value = input.Value
            }).ToList();

            var activity = new PublishingActivity
            {
                Type = PublishingActivityTypes.Prompt,
                Data = new PublishingActivityData
                {
                    Id = interaction.InteractionId.ToString(CultureInfo.InvariantCulture),
                    StatusText = interaction.Message ?? $"{interaction.Title}: ",
                    CompletionState = ToBackchannelCompletionState(CompletionState.InProgress),
                    Inputs = promptInputs
                }
            };

            await ActivityItemUpdated.Writer.WriteAsync(activity, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task CompleteInteractionAsync(string promptId, string?[]? responses, CancellationToken cancellationToken = default)
    {
        if (int.TryParse(promptId, CultureInfo.InvariantCulture, out var interactionId))
        {
            await _interactionService.CompleteInteractionAsync(interactionId,
                (interaction, serviceProvider, cancellationToken) =>
                {
                    if (interaction.InteractionInfo is Interaction.InputsInteractionInfo inputsInfo)
                    {
                        // Set values for all inputs if we have responses
                        if (responses is not null)
                        {
                            for (var i = 0; i < Math.Min(inputsInfo.Inputs.Count, responses.Length); i++)
                            {
                                inputsInfo.Inputs[i].SetValue(responses[i] ?? "");
                            }
                        }

                        return Task.FromResult(new InteractionCompletionState
                        {
                            Complete = true,
                            State = inputsInfo.Inputs
                        });
                    }

                    return Task.FromResult(new InteractionCompletionState
                    {
                        Complete = true,
                        State = null
                    });
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        try
        {
            await _interactionServiceSubscriber.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        _cancellationTokenSource.Dispose();
    }

    internal Channel<PublishingActivity> ActivityItemUpdated { get; } = Channel.CreateUnbounded<PublishingActivity>();
}
