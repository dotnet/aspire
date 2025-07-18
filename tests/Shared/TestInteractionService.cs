// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Aspire.Hosting.Tests;

internal sealed record InteractionData(string Title, string? Message, IReadOnlyList<InteractionInput> Inputs, InteractionOptions? Options, TaskCompletionSource<object> CompletionTcs);

internal sealed class TestInteractionService : IInteractionService
{
    public Channel<InteractionData> Interactions { get; } = Channel.CreateUnbounded<InteractionData>();

    public bool IsAvailable { get; set; } = true;

    public Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = new InteractionData(title, message, inputs, options, new TaskCompletionSource<object>());
        Interactions.Writer.TryWrite(data);
        return (InteractionResult<IReadOnlyList<InteractionInput>>)await data.CompletionTcs.Task;
    }

    public async Task<InteractionResult<bool>> PromptNotificationAsync(string title, string message, NotificationInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = new InteractionData(title, message, [], options, new TaskCompletionSource<object>());
        Interactions.Writer.TryWrite(data);
        return (InteractionResult<bool>)await data.CompletionTcs.Task;
    }

    public Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
