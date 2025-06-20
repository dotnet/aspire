// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

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
    private readonly ILogger<InteractionService> _logger;

    internal InteractionService(ILogger<InteractionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Prompts the user for confirmation with a dialog.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="options">Optional configuration for the message box interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing <c>true</c> if the user confirmed, <c>false</c> otherwise.
    /// </returns>
    public async Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.Intent = MessageIntent.Confirmation;
        options.ShowDismiss = false;
        options.ShowSecondaryButton = true;

        options.PrimaryButtonText ??= "Yes";
        options.SecondaryButtonText ??= "No";

        return await PromptMessageBoxCoreAsync(title, message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Prompts the user with a message box dialog.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message to display in the message box.</param>
    /// <param name="options">Optional configuration for the message box interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing <c>true</c> if the user accepted, <c>false</c> otherwise.
    /// </returns>
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

        var newState = new Interaction(title, message, options, new Interaction.MessageBoxInteractionInfo(intent: options.Intent ?? MessageIntent.None), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        return completion.Canceled
            ? InteractionResultFactory.Cancel<bool>()
            : InteractionResultFactory.Ok((bool)completion.State!);
    }

    /// <summary>
    /// Prompts the user for a single text input.
    /// </summary>
    /// <param name="title">The title of the input dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="inputLabel">The label for the input field.</param>
    /// <param name="placeHolder">The placeholder text for the input field.</param>
    /// <param name="options">Optional configuration for the input dialog interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing the user's input.
    /// </returns>
    public async Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await PromptInputAsync(title, message, new InteractionInput { InputType = InputType.Text, Label = inputLabel, Required = true, Placeholder = placeHolder }, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Prompts the user for a single input using a specified <see cref="InteractionInput"/>.
    /// </summary>
    /// <param name="title">The title of the input dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="input">The input configuration.</param>
    /// <param name="options">Optional configuration for the input dialog interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing the user's input.
    /// </returns>
    public async Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = await PromptInputsAsync(title, message, [input], options, cancellationToken).ConfigureAwait(false);
        if (result.Canceled)
        {
            return InteractionResultFactory.Cancel<InteractionInput>();
        }

        return InteractionResultFactory.Ok(result.Data![0]);
    }

    /// <summary>
    /// Prompts the user for multiple inputs.
    /// </summary>
    /// <param name="title">The title of the input dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="inputs">A collection of <see cref="InteractionInput"/> to prompt for.</param>
    /// <param name="options">Optional configuration for the input dialog interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing the user's inputs.
    /// </returns>
    public async Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IEnumerable<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputList = inputs.ToList();
        options ??= InputsDialogInteractionOptions.Default;

        var newState = new Interaction(title, message, options, new Interaction.InputsInteractionInfo(inputList), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        return completion.Canceled
            ? InteractionResultFactory.Cancel<IReadOnlyList<InteractionInput>>()
            : InteractionResultFactory.Ok((IReadOnlyList<InteractionInput>)completion.State!);
    }

    /// <summary>
    /// Prompts the user with a message bar notification.
    /// </summary>
    /// <param name="title">The title of the message bar.</param>
    /// <param name="message">The message to display in the message bar.</param>
    /// <param name="options">Optional configuration for the message bar interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="InteractionResult{T}"/> containing <c>true</c> if the user accepted, <c>false</c> otherwise.
    /// </returns>
    public async Task<InteractionResult<bool>> PromptMessageBarAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        options ??= MessageBoxInteractionOptions.CreateDefault();

        var newState = new Interaction(title, message, options, new Interaction.MessageBarInteractionInfo(intent: options.Intent ?? MessageIntent.None), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        return completion.Canceled
            ? InteractionResultFactory.Cancel<bool>()
            : InteractionResultFactory.Ok((bool)completion.State!);
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
        interactionState.CompletionTcs.TrySetResult(new InteractionCompletionState { Canceled = true });
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
                    interactionUpdate.CompletionTcs.Task.IsCompleted,
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

    internal void CompleteInteraction(int interactionId, Func<Interaction, InteractionCompletionState> createResult)
    {
        lock (_onInteractionUpdatedLock)
        {
            if (_interactionCollection.TryGetValue(interactionId, out var interactionState))
            {
                var result = createResult(interactionState);

                interactionState.CompletionTcs.TrySetResult(result);
                interactionState.State = Interaction.InteractionState.Complete;
                _interactionCollection.Remove(interactionId);
                OnInteractionUpdated?.Invoke(interactionState);
            }
            else
            {
                _logger.LogDebug("No interaction found with ID {InteractionId}.", interactionId);
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

/// <summary>
/// Represents an input for an interaction.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class InteractionInput
{
    private string? _value;

    /// <summary>
    /// Gets or sets the label for the input.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets the type of the input.
    /// </summary>
    public required InputType InputType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the input is required.
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// Gets or sets the options for the input. Only used by <see cref="InputType.Select"/> inputs.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>>? Options { get; init; }

    /// <summary>
    /// Gets or sets the value of the input.
    /// </summary>
    public string? Value { get => _value; init => _value = value; }

    /// <summary>
    /// Gets or sets the placeholder text for the input.
    /// </summary>
    public string? Placeholder { get; set; }

    internal void SetValue(string value) => _value = value;
}

/// <summary>
/// Specifies the type of input for an <see cref="InteractionInput"/>.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum InputType
{
    /// <summary>
    /// A single-line text input.
    /// </summary>
    Text,
    /// <summary>
    /// A password input.
    /// </summary>
    Password,
    /// <summary>
    /// A select input.
    /// </summary>
    Select,
    /// <summary>
    /// A checkbox input.
    /// </summary>
    Checkbox,
    /// <summary>
    /// A numeric input.
    /// </summary>
    Number
}

/// <summary>
/// Options for configuring an inputs dialog interaction.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class InputsDialogInteractionOptions : InteractionOptions
{
    internal static new InputsDialogInteractionOptions Default { get; } = new();
}

/// <summary>
/// Options for configuring a message box interaction.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class MessageBoxInteractionOptions : InteractionOptions
{
    internal static MessageBoxInteractionOptions CreateDefault() => new();

    /// <summary>
    /// Gets or sets the intent of the message box.
    /// </summary>
    public MessageIntent? Intent { get; set; }
}

/// <summary>
/// Options for configuring a message bar interaction.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class MessageBarInteractionOptions : InteractionOptions
{
    internal static MessageBarInteractionOptions CreateDefault() => new();

    /// <summary>
    /// Gets or sets the intent of the message bar.
    /// </summary>
    public MessageIntent? Intent { get; set; }
}

/// <summary>
/// Specifies the intent or purpose of a message in an interaction.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public enum MessageIntent
{
    /// <summary>
    /// No specific intent.
    /// </summary>
    None = 0,
    /// <summary>
    /// Indicates a successful operation.
    /// </summary>
    Success = 1,
    /// <summary>
    /// Indicates a warning.
    /// </summary>
    Warning = 2,
    /// <summary>
    /// Indicates an error.
    /// </summary>
    Error = 3,
    /// <summary>
    /// Provides informational content.
    /// </summary>
    Information = 4,
    /// <summary>
    /// Requests confirmation from the user.
    /// </summary>
    Confirmation = 5
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

    /// <summary>
    /// Gets or sets a value indicating whether show the secondary button. Defaults to <c>true</c>.
    /// </summary>
    public bool ShowSecondaryButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether show the dismiss button in the header.
    /// </summary>
    public bool ShowDismiss { get; set; } = true;
}

[DebuggerDisplay("State = {State}, Canceled = {Canceled}")]
internal sealed class InteractionCompletionState
{
    public bool Canceled { get; init; }
    public object? State { get; init; }
}

[DebuggerDisplay("InteractionId = {InteractionId}, State = {State}, Title = {Title}")]
internal class Interaction
{
    private static int s_nextInteractionId = 1;

    public int InteractionId { get; }
    public InteractionState State { get; set; }
    public TaskCompletionSource<InteractionCompletionState> CompletionTcs { get; } = new TaskCompletionSource<InteractionCompletionState>(TaskCreationOptions.RunContinuationsAsynchronously);
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
        public MessageBoxInteractionInfo(MessageIntent intent)
        {
            Intent = intent;
        }

        public MessageIntent Intent { get; }
    }

    internal sealed class MessageBarInteractionInfo : InteractionInfoBase
    {
        public MessageBarInteractionInfo(MessageIntent intent)
        {
            Intent = intent;
        }

        public MessageIntent Intent { get; }
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
