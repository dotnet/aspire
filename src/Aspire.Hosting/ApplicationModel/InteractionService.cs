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

    public async Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.Icon ??= MessageBoxIcon.Question;
        options.PrimaryButtonText ??= "Yes";
        options.SecondaryButtonText ??= "No";
        options.ShowDismiss = false;

        return await PromptMessageBoxCoreAsync(title, message, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.ShowSecondaryButton = false;
        options.ShowDismiss = false;

        return await PromptMessageBoxCoreAsync(title, message, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InteractionResult<bool>> PromptMessageBoxCoreAsync(string title, string message, MessageBoxInteractionOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.ShowDismiss = false;

        var newState = new Interaction(title, message, options, new Interaction.MessageBoxInteractionInfo(icon: options.Icon ?? MessageBoxIcon.None), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.TaskCompletionSource.Task.ConfigureAwait(false);
        return completion.Canceled
            ? InteractionResultFactory.Cancel<bool>()
            : InteractionResultFactory.Ok((bool)completion.State!);
    }

    public async Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await PromptInputAsync(title, message, new InteractionInput { InputType = InputType.Text, Label = inputLabel, Required = true, Placeholder = placeHolder }, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = await PromptInputsAsync(title, message, [input], options, cancellationToken).ConfigureAwait(false);
        if (result.Canceled)
        {
            return InteractionResultFactory.Cancel<InteractionInput>();
        }

        return InteractionResultFactory.Ok(result.Data![0]);
    }

    public async Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IEnumerable<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputList = inputs.ToList();
        options ??= InputsDialogInteractionOptions.Default;

        var newState = new Interaction(title, message, options, new Interaction.InputsInteractionInfo(inputList), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.TaskCompletionSource.Task.ConfigureAwait(false);

        return completion.Canceled
            ? InteractionResultFactory.Cancel<IReadOnlyList<InteractionInput>>()
            : InteractionResultFactory.Ok((IReadOnlyList<InteractionInput>)completion.State!);
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
        interactionState.TaskCompletionSource.TrySetResult(new InteractionCompetion { Canceled = true });
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

    internal void CompleteInteraction(int interactionId, Func<Interaction, InteractionCompetion> createResult)
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
public class InteractionResult<T>
{
    /// <summary>
    /// 
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool Canceled { get; }

    internal InteractionResult(T? data, bool canceled)
    {
        Data = data;
        Canceled = canceled;
    }
}

internal static class InteractionResultFactory
{
    internal static InteractionResult<T> Ok<T>(T result)
    {
        return new InteractionResult<T>(result, canceled: false);
    }

    internal static InteractionResult<T> Cancel<T>(T? data = default)
    {
        return new InteractionResult<T>(data ?? default, canceled: true);
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

[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class InputsDialogInteractionOptions : InteractionOptions
{
    internal static new InputsDialogInteractionOptions Default { get; } = new();
}

[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class MessageBoxInteractionOptions : InteractionOptions
{
    internal static MessageBoxInteractionOptions CreateDefault() => new();

    public MessageBoxIcon? Icon { get; set; }
}

[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum MessageBoxIcon
{
    None = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Information = 4,
    Question = 5
}

/// <summary>
/// Optional configuration for interactions added with <see cref="InteractionService"/>.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class InteractionOptions
{
    internal static InteractionOptions Default { get; } = new();

    /// <summary>
    /// Optional primary button text to override the default text.
    /// </summary>
    public string? PrimaryButtonText { get; set; }

    /// <summary>
    /// Optional secondary button text to override the default text.
    /// </summary>
    public string? SecondaryButtonText { get; set; }

    public bool ShowSecondaryButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether show the dismiss button in the header.
    /// </summary>
    public bool ShowDismiss { get; set; } = true;
}

internal sealed class InteractionCompetion
{
    public bool Canceled { get; init; }
    public object? State { get; init; }
}

internal class Interaction
{
    private static int s_nextInteractionId = 1;

    public int InteractionId { get; }
    public InteractionState State { get; set; }
    public TaskCompletionSource<InteractionCompetion> TaskCompletionSource { get; } = new TaskCompletionSource<InteractionCompetion>(TaskCreationOptions.RunContinuationsAsynchronously);
    public InteractionInfoBase InteractionInfo { get; }
    public CancellationToken CancellationToken { get; }

    public string Title { get; }
    public string? Message { get; }
    public InteractionOptions Options { get; }

    public Interaction(string title, string? message, InteractionOptions options, InteractionInfoBase interactionInfo, CancellationToken cancellationToken)
    {
        InteractionId = Interlocked.Increment(ref s_nextInteractionId);
        Title = title;
        Message = message;
        Options = options;
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

    internal sealed class MessageBoxInteractionInfo : InteractionInfoBase
    {
        public MessageBoxInteractionInfo(MessageBoxIcon icon)
        {
            Icon = icon;
        }

        public MessageBoxIcon Icon { get; }
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
