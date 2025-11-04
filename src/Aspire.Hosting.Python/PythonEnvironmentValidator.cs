// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Python;

/// <summary>
/// Validates that the Python environment (virtual environment and/or uv) is properly configured.
/// This validator checks for the virtual environment existence and uv installation when required.
/// </summary>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class PythonEnvironmentValidator(
    IInteractionService interactionService,
    ILogger<PythonEnvironmentValidator> logger) : CoalescingAsyncOperation
{
    private readonly IInteractionService _interactionService = interactionService;
    private readonly ILogger _logger = logger;
    private Task? _notificationTask;
    private string? _notificationMessage;

    /// <summary>
    /// Validates the Python environment for the given resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync(cancellationToken);
    }

    protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        // This method is called once for all Python resources due to coalescing.
        // We don't validate specific resources here because we want to show a single
        // notification for all Python resources.
        
        // For now, we'll implement a simple check that will be triggered when
        // the first Python resource starts.
        
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Validates a specific Python resource environment.
    /// </summary>
    internal async Task<(bool IsValid, string? ValidationMessage)> ValidateResourceAsync(
        PythonAppResource resource,
        CancellationToken cancellationToken = default)
    {
        var notificationTask = _notificationTask;
        if (notificationTask is { IsCompleted: false })
        {
            // Notification is still being shown, just return the cached message
            return (false, _notificationMessage);
        }

        // Check if the resource uses uv
        var usesUv = resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var pythonEnv) && pythonEnv.Uv;

        // If using uv, check if uv is installed
        if (usesUv)
        {
            var uvInstalled = RequiredCommandValidator.ResolveCommand("uv") is not null;
            if (!uvInstalled)
            {
                var message = Resources.MessageStrings.UvNotFoundWhenRequired;
                _logger.LogWarning("{Message}", message);
                _notificationMessage = message;

                if (_interactionService.IsAvailable == true)
                {
                    try
                    {
                        var options = new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Warning,
                            LinkText = "Installation instructions",
                            LinkUrl = "https://docs.astral.sh/uv/getting-started/installation/",
                            ShowDismiss = true,
                            ShowSecondaryButton = false
                        };

                        _notificationTask = _interactionService.PromptNotificationAsync(
                            title: "Missing uv command",
                            message: message,
                            options,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to show missing uv notification");
                    }
                }

                return (false, message);
            }
        }

        // Check if the virtual environment exists
        if (resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var envAnnotation) &&
            envAnnotation.VirtualEnvironment is not null)
        {
            var venvPath = envAnnotation.VirtualEnvironment.VirtualEnvironmentPath;
            if (!Directory.Exists(venvPath))
            {
                // Extract the virtual environment folder name for the user message
                var venvName = Path.GetFileName(venvPath);
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MessageStrings.VirtualEnvironmentNotFound,
                    venvPath,
                    venvName);

                _logger.LogWarning("{Message}", message);
                _notificationMessage = message;

                if (_interactionService.IsAvailable == true)
                {
                    try
                    {
                        var options = new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Warning,
                            LinkText = "Python virtual environments",
                            LinkUrl = "https://docs.python.org/3/library/venv.html",
                            ShowDismiss = true,
                            ShowSecondaryButton = false
                        };

                        _notificationTask = _interactionService.PromptNotificationAsync(
                            title: "Python virtual environment not found",
                            message: message,
                            options,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to show virtual environment not found notification");
                    }
                }

                return (false, message);
            }
        }

        // All checks passed
        _notificationMessage = null;
        return (true, null);
    }
}
#pragma warning restore ASPIREINTERACTION001
