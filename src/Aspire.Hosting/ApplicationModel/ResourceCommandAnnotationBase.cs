// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a command annotation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public abstract class ResourceCommandAnnotationBase : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceCommandAnnotation"/> class.
    /// </summary>
    public ResourceCommandAnnotationBase(
        string name,
        string displayName,
        string? displayDescription,
        object? parameter,
        string? confirmationMessage,
        string? iconName,
        IconVariant? iconVariant,
        bool isHighlighted)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(displayName);

        Name = name;
        DisplayName = displayName;
        DisplayDescription = displayDescription;
        Parameter = parameter;
        ConfirmationMessage = confirmationMessage;
        IconName = iconName;
        IconVariant = iconVariant;
        IsHighlighted = isHighlighted;
    }

    /// <summary>
    /// The name of command. The name uniquely identifies the command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The display name visible in UI.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Optional description of the command, to be shown in the UI.
    /// Could be used as a tooltip. May be localized.
    /// </summary>
    public string? DisplayDescription { get; }

    /// <summary>
    /// Optional parameter that configures the command in some way.
    /// Clients must return any value provided by the server when invoking the command.
    /// </summary>
    public object? Parameter { get; }

    /// <summary>
    /// When a confirmation message is specified, the UI will prompt with an OK/Cancel dialog
    /// and the confirmation message before starting the command.
    /// </summary>
    public string? ConfirmationMessage { get; }

    /// <summary>
    /// The icon name for the command. The name should be a valid FluentUI icon name. https://aka.ms/fluentui-system-icons
    /// </summary>
    public string? IconName { get; }

    /// <summary>
    /// The icon variant for the command.
    /// </summary>
    public IconVariant? IconVariant { get; }

    /// <summary>
    /// A flag indicating whether the command is highlighted in the UI.
    /// </summary>
    public bool IsHighlighted { get; }
}

/// <summary>
/// The icon variant.
/// </summary>
public enum IconVariant
{
    /// <summary>
    /// Regular variant of icons.
    /// </summary>
    Regular,
    /// <summary>
    /// Filled variant of icons.
    /// </summary>
    Filled
}

/// <summary>
/// A factory for <see cref="ExecuteCommandResult"/>.
/// </summary>
public static class CommandResults
{
    /// <summary>
    /// Produces a success result.
    /// </summary>
    public static ExecuteCommandResult Success() => new() { Success = true };

    /// <summary>
    /// Produces an unsuccessful result with an error message.
    /// </summary>
    /// <param name="errorMessage">An optional error message.</param>
    public static ExecuteCommandResult Failure(string? errorMessage = null) => new() { Success = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Produces an unsuccessful result from an <see cref="Exception"/>. <see cref="Exception.Message"/> is used as the error message.
    /// </summary>
    /// <param name="exception">The exception to get the error message from.</param>
    public static ExecuteCommandResult Failure(Exception exception) => Failure(exception.Message);
}

/// <summary>
/// The result of executing a command. Returned from <see cref="ResourceCommandAnnotation.ExecuteCommand"/>.
/// </summary>
public sealed class ExecuteCommandResult
{
    /// <summary>
    /// A flag that indicates whether the command was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// An optional error message that can be set when the command is unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Context for <see cref="ResourceCommandAnnotation.UpdateState"/>.
/// </summary>
public sealed class UpdateCommandStateContext
{
    /// <summary>
    /// The resource snapshot.
    /// </summary>
    public required CustomResourceSnapshot ResourceSnapshot { get; init; }

    /// <summary>
    /// The service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }
}

/// <summary>
/// Context for <see cref="ResourceCommandAnnotation.ExecuteCommand"/>.
/// </summary>
public sealed class ExecuteCommandContext
{
    /// <summary>
    /// The service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The resource name.
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// The cancellation token.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}
