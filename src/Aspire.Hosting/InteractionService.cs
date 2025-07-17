// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal class InteractionService : IInteractionService
{
    internal const string DiagnosticId = "ASPIREINTERACTION001";

    private Action<Interaction>? OnInteractionUpdated { get; set; }
    private readonly object _onInteractionUpdatedLock = new();
    private readonly InteractionCollection _interactionCollection = new();
    private readonly ILogger<InteractionService> _logger;
    private readonly DistributedApplicationOptions _distributedApplicationOptions;
    private readonly IServiceProvider _serviceProvider;

    public InteractionService(ILogger<InteractionService> logger, DistributedApplicationOptions distributedApplicationOptions, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _distributedApplicationOptions = distributedApplicationOptions;
        _serviceProvider = serviceProvider;
    }

    public bool IsAvailable => !_distributedApplicationOptions.DisableDashboard;

    public async Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.Intent = MessageIntent.Confirmation;
        options.ShowDismiss ??= false;
        options.ShowSecondaryButton ??= true;

        return await PromptMessageBoxCoreAsync(title, message, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.ShowSecondaryButton ??= false;
        options.ShowDismiss ??= false;

        return await PromptMessageBoxCoreAsync(title, message, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InteractionResult<bool>> PromptMessageBoxCoreAsync(string title, string message, MessageBoxInteractionOptions options, CancellationToken cancellationToken)
    {
        EnsureServiceAvailable();

        cancellationToken.ThrowIfCancellationRequested();

        options ??= MessageBoxInteractionOptions.CreateDefault();
        options.ShowDismiss ??= false;

        var newState = new Interaction(title, message, options, new Interaction.MessageBoxInteractionInfo(intent: options.Intent ?? MessageIntent.None), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        var promptState = completion.State as bool?;
        return promptState == null
            ? InteractionResult.Cancel<bool>()
            : InteractionResult.Ok(promptState.Value);
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
            return InteractionResult.Cancel<InteractionInput>();
        }

        return InteractionResult.Ok(result.Data[0]);
    }

    public async Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        options ??= InputsDialogInteractionOptions.Default;

        var newState = new Interaction(title, message, options, new Interaction.InputsInteractionInfo(inputs), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        var inputState = completion.State as IReadOnlyList<InteractionInput>;
        return inputState == null
            ? InteractionResult.Cancel<IReadOnlyList<InteractionInput>>()
            : InteractionResult.Ok(inputState);
    }

    public async Task<InteractionResult<bool>> PromptNotificationAsync(string title, string message, NotificationInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureServiceAvailable();

        cancellationToken.ThrowIfCancellationRequested();

        options ??= NotificationInteractionOptions.CreateDefault();

        var newState = new Interaction(title, message, options, new Interaction.NotificationInteractionInfo(intent: options.Intent ?? MessageIntent.None, linkText: options.LinkText, linkUrl: options.LinkUrl), cancellationToken);
        AddInteractionUpdate(newState);

        using var _ = cancellationToken.Register(OnInteractionCancellation, state: newState);

        var completion = await newState.CompletionTcs.Task.ConfigureAwait(false);
        var promptState = completion.State as bool?;
        return promptState == null
            ? InteractionResult.Cancel<bool>()
            : InteractionResult.Ok(promptState.Value);
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
        interactionState.CompletionTcs.TrySetResult(new InteractionCompletionState { Complete = true });
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

    internal async Task CompleteInteractionAsync(int interactionId, Func<Interaction, IServiceProvider, InteractionCompletionState> createResult, CancellationToken cancellationToken)
    {
        Interaction? interactionState = null;

        lock (_onInteractionUpdatedLock)
        {
            if (!_interactionCollection.TryGetValue(interactionId, out interactionState))
            {
                _logger.LogDebug("No interaction found with ID {InteractionId}.", interactionId);
                return;
            }
        }

        var result = createResult(interactionState, _serviceProvider);

        // Run validation for inputs interaction.
        if (!await RunValidationAsync(interactionState, result, cancellationToken).ConfigureAwait(false))
        {
            // Interaction is not complete if there are validation errors.
            result = new InteractionCompletionState { Complete = false, State = result.State };
        }

        lock (_onInteractionUpdatedLock)
        {
            // Double check interaction is still in collection after awaiting the result creation.
            if (!_interactionCollection.TryGetValue(interactionId, out interactionState))
            {
                return;
            }

            if (result.Complete)
            {
                interactionState.CompletionTcs.TrySetResult(result);
                interactionState.State = Interaction.InteractionState.Complete;
                _interactionCollection.Remove(interactionId);
            }

            // Either broadcast out the interaction is complete, or its updated state.
            OnInteractionUpdated?.Invoke(interactionState);
        }
    }

    /// <summary>
    /// Runs validation for the inputs interaction.
    /// </summary>
    /// <returns>
    /// true if validation passed, false if there were validation errors.
    /// </returns>
    private async Task<bool> RunValidationAsync(Interaction interactionState, InteractionCompletionState result, CancellationToken cancellationToken)
    {
        if (result.Complete && interactionState.InteractionInfo is Interaction.InputsInteractionInfo inputsInfo)
        {
            // State could be null if the user dismissed the inputs dialog. There is nothing to validate in this situation.
            if (result.State is IReadOnlyList<InteractionInput> inputs)
            {
                var options = (InputsDialogInteractionOptions)interactionState.Options;

                if (options.ValidationCallback is { } validationCallback)
                {
                    foreach (var input in inputs)
                    {
                        input.ValidationErrors.Clear();
                    }

                    var context = new InputsDialogValidationContext
                    {
                        CancellationToken = cancellationToken,
                        ServiceProvider = _serviceProvider,
                        Inputs = inputsInfo.Inputs
                    };
                    await validationCallback(context).ConfigureAwait(false);

                    return !context.HasErrors;
                }
            }
        }

        return true;
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

    private void EnsureServiceAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException($"InteractionService is not available because the dashboard is not enabled. Use the {nameof(IsAvailable)} property to determine whether the service is available.");
        }
    }
}

internal class InteractionCollection : KeyedCollection<int, Interaction>
{
    protected override int GetKeyForItem(Interaction item) => item.InteractionId;
}

[DebuggerDisplay("State = {State}, Complete = {Complete}")]
internal sealed class InteractionCompletionState
{
    public bool Complete { get; init; }
    public object? State { get; init; }
}

[DebuggerDisplay("InteractionId = {InteractionId}, State = {State}, Title = {Title}")]
internal class Interaction
{
    private static int s_nextInteractionId;

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

    internal sealed class NotificationInteractionInfo : InteractionInfoBase
    {
        public NotificationInteractionInfo(MessageIntent intent, string? linkText, string? linkUrl)
        {
            Intent = intent;
            LinkText = linkText;
            LinkUrl = linkUrl;
        }

        public MessageIntent Intent { get; }
        public string? LinkText { get; }
        public string? LinkUrl { get; }
    }

    internal sealed class InputsInteractionInfo : InteractionInfoBase
    {
        public InputsInteractionInfo(IReadOnlyList<InteractionInput> inputs)
        {
            Inputs = inputs;
        }

        public IReadOnlyList<InteractionInput> Inputs { get; }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
