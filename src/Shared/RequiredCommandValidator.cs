// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Base class that extends <see cref="CoalescingAsyncOperation"/> with validation logic
/// ensuring that a command (executable) path supplied by an implementation is valid
/// for launching a new process. The command is considered valid if either:
/// 1. It is an absolute or relative path (contains a directory separator) that points to an existing file, or
/// 2. It is discoverable on the current process PATH (respecting PATHEXT on Windows).
///
/// Once validated, the resolved full path is passed to <see cref="OnValidatedAsync"/> for
/// any additional (optional) work by derived classes.
///
/// Use the inherited RunAsync method to coalesce concurrent validation requests.
/// </summary>
// Suppress experimental interaction API warnings locally.
#pragma warning disable ASPIREINTERACTION001
internal abstract class RequiredCommandValidator(IInteractionService interactionService, ILogger logger) : CoalescingAsyncOperation
{
    private readonly IInteractionService _interactionService = interactionService;
    private readonly ILogger _logger = logger;

    private Task? _notificationTask;
    private string? _notificationMessage;

    /// <summary>
    /// Returns the command string (file name or path) that should be validated.
    /// </summary>
    protected abstract string GetCommandPath();

    /// <summary>
    /// Gets the message to display when the command is not found and there is no help link.
    /// Default: "Required command '{0}' was not found on PATH or at a specified location."
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <returns>The formatted message.</returns>
    protected virtual string GetCommandNotFoundMessage(string command) =>
        string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at a specified location.", command);

    /// <summary>
    /// Gets the message to display when the command is not found and there is a help link.
    /// Default: "Required command '{0}' was not found. See installation instructions for more details."
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <returns>The formatted message.</returns>
    protected virtual string GetCommandNotFoundWithLinkMessage(string command) =>
        string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found. See installation instructions for more details.", command);

    /// <summary>
    /// Gets the message to display when the command is found but validation failed.
    /// Default: "{0} See installation instructions for more details."
    /// </summary>
    /// <param name="validationMessage">The validation failure message.</param>
    /// <returns>The formatted message.</returns>
    protected virtual string GetValidationFailedMessage(string validationMessage) =>
        string.Format(CultureInfo.CurrentCulture, "{0} See installation instructions for more details.", validationMessage);

    /// <summary>
    /// Called after the command has been successfully resolved to a full path.
    /// Default implementation does nothing.
    /// </summary>
    /// <remarks>
    /// Overrides can perform additional validation to verify the command is usable.
    /// </remarks>
    protected virtual Task<(bool IsValid, string? ValidationMessage)> OnResolvedAsync(string resolvedCommandPath, CancellationToken cancellationToken) => Task.FromResult((true, (string?)null));

    /// <summary>
    /// Called after the command has been successfully validated and resolved to a full path.
    /// Default implementation does nothing.
    /// </summary>
    /// <param name="resolvedCommandPath">The resolved full filesystem path to the executable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    protected sealed override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        var command = GetCommandPath();

        var notificationTask = _notificationTask;
        if (notificationTask is { IsCompleted: false })
        {
            // Failure notification is still being shown so just throw again.
            throw new DistributedApplicationException(_notificationMessage ?? $"Required command '{command}' was not found on PATH, at the specified location, or failed validation.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException("Command path cannot be null or empty.");
        }
        var resolved = ResolveCommand(command);
        var isValid = true;
        string? validationMessage = null;
        if (resolved is not null)
        {
            (isValid, validationMessage) = await OnResolvedAsync(resolved, cancellationToken).ConfigureAwait(false);
        }
        if (resolved is null || !isValid)
        {
            var link = GetHelpLink();
            var message = (link, validationMessage) switch
            {
                (null, not null) => validationMessage,
                (not null, not null) => GetValidationFailedMessage(validationMessage),
                (not null, null) => GetCommandNotFoundWithLinkMessage(command),
                _ => GetCommandNotFoundMessage(command)
            };

            _logger.LogWarning("{Message}", message);

            _notificationMessage = message;
            if (_interactionService.IsAvailable == true)
            {
                try
                {
                    var options = new NotificationInteractionOptions
                    {
                        Intent = MessageIntent.Warning,
                        // Provide a link only if we have one.
                        LinkText = link is null ? null : "Installation instructions",
                        LinkUrl = link,
                        ShowDismiss = true,
                        ShowSecondaryButton = false
                    };

                    _notificationTask = _interactionService.PromptNotificationAsync(
                        title: "Missing command",
                        message: message,
                        options,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to show missing command notification");
                }
            }
            throw new DistributedApplicationException(message);
        }

        _notificationMessage = null;
        await OnValidatedAsync(resolved, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Optional link returned to guide users when the command is missing. Return null for no link.
    /// </summary>
    protected virtual string? GetHelpLink() => null;

    /// <summary>
    /// Attempts to resolve a command (file name or path) to a full path.
    /// </summary>
    /// <param name="command">The command string.</param>
    /// <returns>Full path if resolved; otherwise null.</returns>
    protected internal static string? ResolveCommand(string command)
    {
        // If the command includes any directory separator, treat it as a path (relative or absolute)
        if (command.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            var candidate = Path.GetFullPath(command);
            return File.Exists(candidate) ? candidate : null;
        }

        // Search PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows consider PATHEXT if no extension specified
            var hasExtension = Path.HasExtension(command);
            var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
            var exts = pathext.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var dir in paths)
            {
                if (hasExtension)
                {
                    var candidate = Path.Combine(dir, command);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                else
                {
                    foreach (var ext in exts)
                    {
                        var candidate = Path.Combine(dir, command + ext);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var dir in paths)
            {
                var candidate = Path.Combine(dir, command);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
#pragma warning restore ASPIREINTERACTION001
