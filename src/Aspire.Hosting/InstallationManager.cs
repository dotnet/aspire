// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

// Suppress experimental interaction API warnings locally.
#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIRECOMMAND001

/// <summary>
/// Service that ensures required commands (executables) are available on the system.
/// </summary>
[Experimental("ASPIRECOMMAND001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IInstallationManager
{
    /// <summary>
    /// Ensures a command is installed and available. Shows a notification to the user if not found.
    /// Only validates each command once per application lifetime.
    /// </summary>
    /// <param name="command">The command name or path to validate.</param>
    /// <param name="helpLink">Optional URL with installation instructions.</param>
    /// <param name="additionalValidation">Optional callback to perform additional validation (e.g., version checks) after the command is found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the command is installed and valid, false otherwise.</returns>
    Task<bool> EnsureInstalledAsync(
        string command,
        string? helpLink = null,
        Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? additionalValidation = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service that ensures required commands (executables) are available on the system.
/// Validates commands once per application lifetime and caches the results.
/// </summary>
internal sealed class InstallationManager : IInstallationManager, IDisposable
{
    private readonly IInteractionService _interactionService;
    private readonly ILogger<InstallationManager> _logger;

    // Cache of command validation results: command name -> is installed
    private readonly ConcurrentDictionary<string, bool> _validationCache = new(StringComparer.OrdinalIgnoreCase);

    // Per-command coalescing operations to prevent concurrent validation attempts
    private readonly ConcurrentDictionary<string, CommandValidator> _validators = new(StringComparer.OrdinalIgnoreCase);

    public InstallationManager(IInteractionService interactionService, ILogger<InstallationManager> logger)
    {
        _interactionService = interactionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> EnsureInstalledAsync(
        string command,
        string? helpLink = null,
        Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? additionalValidation = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));
        }

        // Check if we've already validated this command
        if (_validationCache.TryGetValue(command, out var cachedResult))
        {
            return cachedResult;
        }

        // Get or create a validator for this command
        var validator = _validators.GetOrAdd(command, cmd => new CommandValidator(
            cmd,
            helpLink,
            additionalValidation,
            _interactionService,
            _logger));

        // Run the validation (coalesces concurrent requests)
        var isValid = await validator.RunAsync(cancellationToken).ConfigureAwait(false);

        // Cache the result
        _validationCache.TryAdd(command, isValid);

        return isValid;
    }

    /// <summary>
    /// Disposes the installation manager and all associated resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var validator in _validators.Values)
        {
            validator.Dispose();
        }
        _validators.Clear();
    }

    /// <summary>
    /// Attempts to resolve a command (file name or path) to a full path.
    /// </summary>
    /// <param name="command">The command string.</param>
    /// <returns>Full path if resolved; otherwise null.</returns>
    private static string? ResolveCommand(string command)
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

    /// <summary>
    /// Internal class that handles validation for a specific command using coalescing async operations.
    /// </summary>
    private sealed class CommandValidator : IDisposable
    {
        private readonly string _command;
        private readonly string? _helpLink;
        private readonly Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? _additionalValidation;
        private readonly IInteractionService _interactionService;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private Task<bool>? _runningTask;
        private CancellationTokenSource? _cts;

        public CommandValidator(
            string command,
            string? helpLink,
            Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>>? additionalValidation,
            IInteractionService interactionService,
            ILogger logger)
        {
            _command = command;
            _helpLink = helpLink;
            _additionalValidation = additionalValidation;
            _interactionService = interactionService;
            _logger = logger;
        }

        public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            Task<bool> current;
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_runningTask is { IsCompleted: false })
                {
                    // Already running, coalesce onto the existing task
                    current = _runningTask;
                }
                else
                {
                    // Start a new execution
                    _cts?.Dispose();
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    current = _runningTask = ExecuteAsync(_cts.Token);

                    _ = _runningTask.ContinueWith(ClearCompleted,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
            finally
            {
                _gate.Release();
            }

            return await current.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            var resolved = ResolveCommand(_command);
            var isValid = true;
            string? validationMessage = null;

            if (resolved is not null && _additionalValidation is not null)
            {
                (isValid, validationMessage) = await _additionalValidation(resolved, cancellationToken).ConfigureAwait(false);
            }

            if (resolved is null || !isValid)
            {
                var message = (_helpLink, validationMessage) switch
                {
                    (null, not null) => validationMessage,
                    (not null, not null) => string.Format(CultureInfo.CurrentCulture, "{0} See installation instructions for more details.", validationMessage),
                    (not null, null) => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found. See installation instructions for more details.", _command),
                    _ => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at a specified location.", _command)
                };

                _logger.LogWarning("{Message}", message);

                if (_interactionService.IsAvailable == true)
                {
                    try
                    {
                        var options = new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Warning,
                            LinkText = _helpLink is null ? null : "Installation instructions",
                            LinkUrl = _helpLink,
                            ShowDismiss = true,
                            ShowSecondaryButton = false
                        };

                        _ = _interactionService.PromptNotificationAsync(
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

                return false;
            }

            return true;
        }

        private void ClearCompleted(Task completed)
        {
            _ = ClearCompletedAsync(completed);
        }

        private async Task ClearCompletedAsync(Task completed)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (ReferenceEquals(completed, _runningTask))
                {
                    _runningTask = null;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Wait();
            try
            {
                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                    // ignored
                }
                _cts?.Dispose();
                _cts = null;
                _runningTask = null;
            }
            finally
            {
                _gate.Release();
                _gate.Dispose();
            }
        }
    }
}
