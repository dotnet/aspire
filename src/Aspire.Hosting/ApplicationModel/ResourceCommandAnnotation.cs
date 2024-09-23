// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a command annotation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Type = {Type}")]
public sealed class ResourceCommandAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceCommandAnnotation"/> class.
    /// </summary>
    public ResourceCommandAnnotation(
        string type,
        string displayName,
        Func<UpdateCommandStateContext, ResourceCommandState> updateState,
        Func<ExecuteCommandContext, Task<ExecuteCommandResult>> executeCommand,
        string? iconName,
        IconVariant? iconVariant,
        bool isHighlighted)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(updateState);
        ArgumentNullException.ThrowIfNull(executeCommand);

        Type = type;
        DisplayName = displayName;
        UpdateState = updateState;
        ExecuteCommand = executeCommand;
        IconName = iconName;
        IconVariant = iconVariant;
        IsHighlighted = isHighlighted;
    }

    /// <summary>
    /// The type of command. The type uniquely identifies the command.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The display name visible in UI.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// A callback that is used to update the command state.
    /// The callback is executed when the command's resource snapshot is updated.
    /// </summary>
    public Func<UpdateCommandStateContext, ResourceCommandState> UpdateState { get; }

    /// <summary>
    /// A callback that is executed when the command is executed.
    /// The result is used to indicate success or failure in the UI.
    /// </summary>
    public Func<ExecuteCommandContext, Task<ExecuteCommandResult>> ExecuteCommand { get; }

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
    public static ExecuteCommandResult Success() => new ExecuteCommandResult { Success = true };
}

/// <summary>
/// The result of executing a command. Returned from <see cref="ResourceCommandAnnotation.ExecuteCommand"/>.
/// </summary>
public class ExecuteCommandResult
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
public class UpdateCommandStateContext
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
public class ExecuteCommandContext
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
