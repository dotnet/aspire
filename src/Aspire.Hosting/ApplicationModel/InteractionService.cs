// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Aspire.Hosting.ApplicationModel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// A service to interact with the current development environment.
/// </summary>
[Experimental(DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class InteractionService
{
    internal const string DiagnosticId = "ASPIREINTERACTION001";

    private Action<Interaction>? OnInteractionUpdated { get; set; }
    private readonly object _onInteractionUpdatedLock = new();
    private readonly InteractionCollection _interactionCollection = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<InteractionResult> PromptConfirmationAsync(string title, string? message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newState = new Interaction(title, message, new Interaction.ConfirmationInteractionInfo(iconName: null), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        return await newState.TaskCompletionSource.Task.ConfigureAwait(false);
    }

    public async Task<InteractionResult> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, CancellationToken cancellationToken = default)
    {
        return await PromptInputAsync(title, message, new InteractionInput { InputType = InputType.Text, Label = inputLabel, Required = true, Placeholder = placeHolder }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InteractionResult> PromptInputAsync(string title, string? message, InteractionInput input, CancellationToken cancellationToken = default)
    {
        return await PromptInputsAsync(title, message, [input], cancellationToken).ConfigureAwait(false);
    }

    public async Task<InteractionResult> PromptInputsAsync(string title, string? message, IEnumerable<InteractionInput> inputs, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputList = inputs.ToList();

        var newState = new Interaction(title, message, new Interaction.InputsInteractionInfo(inputList), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        return await newState.TaskCompletionSource.Task.ConfigureAwait(false);
    }

    // For testing.
    internal List<Interaction> GetCurrentInteractions()
    {
        lock (_onInteractionUpdatedLock)
        {
            return _interactionCollection.ToList();
        }
    }

    private void OnInteractionCancellation(object? newState)
    {
        var interactionState = (Interaction)newState!;

        interactionState.State = Interaction.InteractionState.Complete;
        interactionState.TaskCompletionSource.TrySetResult(InteractionResult.Cancel());
        AddInteractionUpdate(interactionState);
    }

    private void AddInteractionUpdate(Interaction interactionUpdate)
    {
        lock (_onInteractionUpdatedLock)
        {
            var updateEvent = false;

            if (interactionUpdate.State == Interaction.InteractionState.Complete)
            {
                Debug.Assert(
                    interactionUpdate.TaskCompletionSource.Task.IsCompleted,
                    "TaskCompletionSource should be completed when interaction is done.");

                // Only update event if interaction was previously registered and not already removed.
                updateEvent = _interactionCollection.Remove(interactionUpdate.InteractionId);
            }
            else
            {
                if (_interactionCollection.Contains(interactionUpdate.InteractionId))
                {
                    // Should never happen, but throw descriptive exception if it does.
                    throw new InvalidOperationException($"An interaction with ID {interactionUpdate.InteractionId} already exists. Interaction IDs must be unique.");
                }

                _interactionCollection.Add(interactionUpdate);
                updateEvent = true;
            }

            if (updateEvent)
            {
                OnInteractionUpdated?.Invoke(interactionUpdate);
            }
        }
    }

    internal void CompleteInteraction(int interactionId, Func<Interaction, InteractionResult> createResult)
    {
        lock (_onInteractionUpdatedLock)
        {
            if (_interactionCollection.TryGetValue(interactionId, out var interactionState))
            {
                var result = createResult(interactionState);

                interactionState.TaskCompletionSource.TrySetResult(result);
                interactionState.State = Interaction.InteractionState.Complete;
                _interactionCollection.Remove(interactionId);
                OnInteractionUpdated?.Invoke(interactionState);
            }
            else
            {
                throw new InvalidOperationException($"No interaction found with ID {interactionId}.");
            }
        }
    }

    internal async IAsyncEnumerable<Interaction> SubscribeInteractionUpdates([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<Interaction>();

        void WriteToChannel(Interaction resourceEvent) =>
            channel.Writer.TryWrite(resourceEvent);

        List<Interaction> pendingInteractions;

        lock (_onInteractionUpdatedLock)
        {
            OnInteractionUpdated += WriteToChannel;

            pendingInteractions = _interactionCollection.ToList();
        }

        foreach (var interaction in pendingInteractions)
        {
            yield return interaction;
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            lock (_onInteractionUpdatedLock)
            {
                OnInteractionUpdated -= WriteToChannel;
            }

            channel.Writer.TryComplete();
        }
    }
}

internal class InteractionCollection : KeyedCollection<int, Interaction>
{
    protected override int GetKeyForItem(Interaction item) => item.InteractionId;
}

/// <summary>
/// 
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class InteractionResult
{
    /// <summary>
    /// 
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool Canceled { get; }

    private InteractionResult(object? data, bool canceled)
    {
        Data = data;
        Canceled = canceled;
    }

    internal static InteractionResult Ok<T>(T result)
    {
        return new InteractionResult(result, canceled: false);
    }

    internal static InteractionResult Cancel(object? data = null)
    {
        return new InteractionResult(data ?? null, canceled: true);
    }
}

[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class InteractionInput
{
    private string? _value;

    public required string Label { get; init; }
    public required InputType InputType { get; init; }
    public bool Required { get; init; }
    public IReadOnlyList<KeyValuePair<string, string>>? Options { get; init; }
    public string? Value { get => _value; init => _value = value; }
    public string? Placeholder { get; set; }

    internal void SetValue(string value) => _value = value;
}

[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum InputType
{
    Text,
    Password,
    Select,
    Checkbox,
    Number
}

internal class Interaction
{
    private static int s_nextInteractionId = 1;

    public int InteractionId { get; }
    public InteractionState State { get; set; }
    public TaskCompletionSource<InteractionResult> TaskCompletionSource { get; } = new TaskCompletionSource<InteractionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    public InteractionInfoBase InteractionInfo { get; }
    public CancellationToken CancellationToken { get; }

    public string Title { get; }
    public string? Message { get; }

    public Interaction(string title, string? message, InteractionInfoBase interactionInfo, CancellationToken cancellationToken)
    {
        InteractionId = Interlocked.Increment(ref s_nextInteractionId);
        Title = title;
        Message = message;
        InteractionInfo = interactionInfo;
        CancellationToken = cancellationToken;
    }

    internal enum InteractionState
    {
        InProgress,
        Complete
    }

    internal abstract class InteractionInfoBase
    {
    }

    internal sealed class ConfirmationInteractionInfo : InteractionInfoBase
    {
        public ConfirmationInteractionInfo(string? iconName)
        {
            IconName = iconName;
        }

        public string? IconName { get; }
    }

    internal sealed class InputsInteractionInfo : InteractionInfoBase
    {
        public InputsInteractionInfo(List<InteractionInput> inputs)
        {
            Inputs = inputs;
        }

        public List<InteractionInput> Inputs { get; }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
