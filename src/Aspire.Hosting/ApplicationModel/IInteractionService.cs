// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// A service to interact with the current development environment.
/// </summary>
[Experimental(InteractionService.DiagnosticId, UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IInteractionService
{
    /// <summary>
    /// Gets a value indicating whether the interaction service is available. If <c>false</c>,
    /// this service is not available to interact with the user and service methods will throw
    /// an exception.
    /// </summary>
    bool IsAvailable { get; }

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
    Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<InteractionResult<bool>> PromptMessageBarAsync(string title, string message, MessageBarInteractionOptions? options = null, CancellationToken cancellationToken = default);
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

    internal List<string> ValidationErrors { get; } = [];
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

    /// <summary>
    /// Gets or sets the validation callback for the inputs dialog. This callback is invoked when the user submits the dialog.
    /// If validation errors are added to the <see cref="InputsDialogValidationContext"/>, the dialog will not close and the user will be prompted to correct the errors.
    /// </summary>
    public Func<InputsDialogValidationContext, Task>? ValidationCallback { get; set; }
}

/// <summary>
/// Represents the context for validating inputs in an inputs dialog interaction.
/// </summary>
public sealed class InputsDialogValidationContext
{
    internal bool HasErrors { get; private set; }

    /// <summary>
    /// Gets the inputs that are being validated.
    /// </summary>
    public required IReadOnlyList<InteractionInput> Inputs { get; init; }

    /// <summary>
    /// Gets the cancellation token for the validation operation.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Gets the service provider for resolving services during validation.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Adds a validation error for the specified input.
    /// </summary>
    /// <param name="input">The input to add a validation error for.</param>
    /// <param name="errorMessage">The error message to add.</param>
    public void AddValidationError(InteractionInput input, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        if (string.IsNullOrEmpty(errorMessage))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
        }

        input.ValidationErrors.Add(errorMessage);
        HasErrors = true;
    }
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

    /// <summary>
    /// Gets or sets the text for a link in the message bar.
    /// </summary>
    public string? LinkText { get; set; }

    /// <summary>
    /// Gets or sets the URL for the link in the message bar.
    /// </summary>
    public string? LinkUrl { get; set; }
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
    /// Gets or sets a value indicating whether show the secondary button.
    /// </summary>
    public bool? ShowSecondaryButton { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether show the dismiss button in the header.
    /// </summary>
    public bool? ShowDismiss { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to escape HTML in the message content. Defaults to <c>true</c>.
    /// </summary>
    public bool? EscapeMessageHtml { get; set; } = true;
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
