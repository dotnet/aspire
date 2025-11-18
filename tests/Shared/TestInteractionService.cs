// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Aspire.Hosting.Tests;

internal sealed record InteractionData(string Title, string? Message, InteractionInputCollection Inputs, InteractionOptions? Options, CancellationToken CancellationToken, TaskCompletionSource<object> CompletionTcs);

internal sealed class TestInteractionService : IInteractionService
{
    public Channel<InteractionData> Interactions { get; } = Channel.CreateUnbounded<InteractionData>();

    public bool IsAvailable { get; set; } = true;

    public InteractionReference<bool> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public InteractionReference<InteractionInput> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public InteractionReference<InteractionInput> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public InteractionReference<InteractionInputCollection> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var data = new InteractionData(title, message, new InteractionInputCollection(inputs), options, cancellationToken, new TaskCompletionSource<object>());
        Interactions.Writer.TryWrite(data);
        
        var resultTask = Task.Run(async () =>
        {
            var result = (InteractionResult<InteractionInputCollection>)await data.CompletionTcs.Task;

            // Convert the result to use InteractionInputCollection
            if (result.Canceled)
            {
                return InteractionResult.Cancel<InteractionInputCollection>();
            }

            return InteractionResult.Ok(new InteractionInputCollection(result.Data));
        }, cts.Token);

        return new InteractionReference<InteractionInputCollection>(resultTask, cts);
    }

    public InteractionReference<bool> PromptNotificationAsync(string title, string message, NotificationInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var data = new InteractionData(title, message, new InteractionInputCollection([]), options, cancellationToken, new TaskCompletionSource<object>());
        Interactions.Writer.TryWrite(data);
        
        var resultTask = Task.Run(async () =>
        {
            return (InteractionResult<bool>)await data.CompletionTcs.Task;
        }, cts.Token);

        return new InteractionReference<bool>(resultTask, cts);
    }

    public InteractionReference<bool> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
