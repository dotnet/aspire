// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREINTERACTION001

using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Channels;
using Aspire.Hosting.Backchannel;

namespace Aspire.Hosting.Publishing;

internal sealed class PublishingActivityReporter : IPublishingActivityReporter, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, PublishingStep> _steps = new();
    private readonly InteractionService _interactionService;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _interactionServiceSubscriber;

    public PublishingActivityReporter(InteractionService interactionService)
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
        var step = new PublishingStep(this, Guid.NewGuid().ToString(), title);
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

    public async Task<PublishingTask> CreateTaskAsync(PublishingStep step, string statusText, CancellationToken cancellationToken)
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

        var task = new PublishingTask(Guid.NewGuid().ToString(), step.Id, statusText, parentStep);

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

    public async Task CompleteStepAsync(PublishingStep step, string completionText, CompletionState completionState, CancellationToken cancellationToken)
    {
        lock (step)
        {
            // Prevent double completion if the step is already complete
            if (step.CompletionState != CompletionState.InProgress)
            {
                throw new InvalidOperationException($"Cannot complete step '{step.Id}' with state '{step.CompletionState}'. Only 'InProgress' steps can be completed.");
            }

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

    public async Task UpdateTaskAsync(PublishingTask task, string statusText, CancellationToken cancellationToken)
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

    public async Task CompleteTaskAsync(PublishingTask task, CompletionState completionState, string? completionMessage, CancellationToken cancellationToken)
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

    public async Task CompletePublishAsync(string? completionMessage = null, CompletionState? completionState = null, bool isDeploy = false, CancellationToken cancellationToken = default)
    {
        // Use provided state or aggregate from all steps
        var finalState = completionState ?? CalculateOverallAggregatedState();

        var operationName = isDeploy ? "Deployment" : "Publishing";
        var state = new PublishingActivity
        {
            Type = PublishingActivityTypes.PublishComplete,
            Data = new PublishingActivityData
            {
                Id = PublishingActivityTypes.PublishComplete,
                StatusText = completionMessage ?? finalState switch
                {
                    CompletionState.Completed => $"{operationName} completed successfully",
                    CompletionState.CompletedWithWarning => $"{operationName} completed with warnings",
                    CompletionState.CompletedWithError => $"{operationName} completed with errors",
                    _ => $"{operationName} completed"
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
        if (interaction.State == Interaction.InteractionState.InProgress)
        {
            if (HasStepsInProgress())
            {
                await _interactionService.CompleteInteractionAsync(interaction.InteractionId, (interaction, ServiceProvider) =>
                {
                    // Complete the interaction with an error state
                    interaction.CompletionTcs.TrySetException(new InvalidOperationException("Cannot prompt interaction while steps are in progress."));
                    return new InteractionCompletionState
                    {
                        Complete = true,
                        State = "Cannot prompt interaction while steps are in progress."
                    };
                }, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Handle input interaction types
            if (interaction.InteractionInfo is Interaction.InputsInteractionInfo inputsInfo && inputsInfo.Inputs.Count > 0)
            {
                var promptInputs = inputsInfo.Inputs.Select(input => new PublishingPromptInput
                {
                    Label = input.Label,
                    InputType = input.InputType.ToString(),
                    Required = input.Required,
                    Options = input.Options,
                    Value = input.Value,
                    ValidationErrors = input.ValidationErrors
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
            // Handle notification interaction types (PromptNotificationAsync)
            else if (interaction.InteractionInfo is Interaction.NotificationInteractionInfo)
            {
                var promptInputs = new List<PublishingPromptInput>
                {
                    new PublishingPromptInput
                    {
                        Label = "Confirm",
                        InputType = "Boolean",
                        Required = true,
                        Options = null,
                        Value = null,
                        ValidationErrors = null
                    }
                };

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
    }

    internal async Task CompleteInteractionAsync(string promptId, PublishingPromptInputAnswer[]? responses, CancellationToken cancellationToken = default)
    {
        if (int.TryParse(promptId, CultureInfo.InvariantCulture, out var interactionId))
        {
            await _interactionService.CompleteInteractionAsync(interactionId,
                (interaction, serviceProvider) =>
                {
                    if (interaction.InteractionInfo is Interaction.InputsInteractionInfo inputsInfo)
                    {
                        // Set values for all inputs if we have responses
                        if (responses is not null)
                        {
                            for (var i = 0; i < Math.Min(inputsInfo.Inputs.Count, responses.Length); i++)
                            {
                                inputsInfo.Inputs[i].Value = responses[i].Value ?? "";
                            }
                        }

                        return new InteractionCompletionState
                        {
                            Complete = true,
                            State = inputsInfo.Inputs
                        };
                    }
                    else if (interaction.InteractionInfo is Interaction.NotificationInteractionInfo)
                    {
                        // Handle notification interactions with boolean result
                        bool result = false;
                        if (responses is not null && responses.Length > 0)
                        {
                            // Parse the boolean value from the first response
                            result = bool.TryParse(responses[0].Value, out var parsedValue) && parsedValue;
                        }

                        return new InteractionCompletionState
                        {
                            Complete = true,
                            State = result
                        };
                    }

                    return new InteractionCompletionState
                    {
                        Complete = true,
                        State = null
                    };
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
