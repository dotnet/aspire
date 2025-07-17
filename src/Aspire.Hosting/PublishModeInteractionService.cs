// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// A wrapper around <see cref="InteractionService"/> that restricts API usage based on the execution context.
/// In publish mode (CLI context), only input prompting methods are allowed. All other methods throw <see cref="InvalidOperationException"/>.
/// </summary>
internal sealed class PublishModeInteractionService : IInteractionService
{
    private readonly InteractionService _innerService;
    private readonly DistributedApplicationExecutionContext _executionContext;

    public PublishModeInteractionService(InteractionService innerService, DistributedApplicationExecutionContext executionContext)
    {
        _innerService = innerService;
        _executionContext = executionContext;
    }

    /// <inheritdoc />
    public bool IsAvailable => _innerService.IsAvailable;

    /// <inheritdoc />
    public Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupportedInPublishMode();
        return _innerService.PromptConfirmationAsync(title, message, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupportedInPublishMode();
        return _innerService.PromptMessageBoxAsync(title, message, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        // This method is allowed in both run and publish modes
        return _innerService.PromptInputAsync(title, message, inputLabel, placeHolder, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        // This method is allowed in both run and publish modes
        return _innerService.PromptInputAsync(title, message, input, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        // This method is allowed in both run and publish modes
        return _innerService.PromptInputsAsync(title, message, inputs, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InteractionResult<bool>> PromptNotificationAsync(string title, string message, NotificationInteractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupportedInPublishMode();
        return _innerService.PromptNotificationAsync(title, message, options, cancellationToken);
    }

    private void ThrowIfUnsupportedInPublishMode()
    {
        if (_executionContext.IsPublishMode)
        {
            throw new InvalidOperationException("This interaction is not supported when running in publish mode (CLI context). Only PromptInputAsync and PromptInputsAsync methods are supported in publish mode.");
        }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.