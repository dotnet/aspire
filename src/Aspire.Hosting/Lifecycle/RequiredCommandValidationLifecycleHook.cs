// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001

using System.Collections.Concurrent;
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// An eventing subscriber that validates required commands are installed before resources start.
/// </summary>
/// <remarks>
/// This subscriber processes <see cref="RequiredCommandAnnotation"/> on resources and validates
/// that the specified commands/executables are available on the local machine PATH.
/// Notifications are coalesced per command to avoid duplicate notifications when multiple
/// resources require the same command.
/// </remarks>
internal sealed class RequiredCommandValidationLifecycleHook(
    IInteractionService interactionService,
    ILogger<RequiredCommandValidationLifecycleHook> logger) : IDistributedApplicationEventingSubscriber
{
    private readonly IInteractionService _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Track validation state per command to coalesce notifications
    private readonly ConcurrentDictionary<string, CommandValidationState> _commandStates = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        // Subscribe to BeforeResourceStartedEvent to validate commands before each resource starts
        eventing.Subscribe<BeforeResourceStartedEvent>(ValidateRequiredCommandsAsync);
        return Task.CompletedTask;
    }

    private async Task ValidateRequiredCommandsAsync(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        var resource = @event.Resource;

        // Get all RequiredCommandAnnotation instances on the resource
        var requiredCommands = resource.Annotations.OfType<RequiredCommandAnnotation>().ToList();

        if (requiredCommands.Count == 0)
        {
            return;
        }

        foreach (var annotation in requiredCommands)
        {
            await ValidateCommandAsync(resource, annotation, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ValidateCommandAsync(
        IResource resource,
        RequiredCommandAnnotation annotation,
        CancellationToken cancellationToken)
    {
        var command = annotation.Command;

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException($"Required command on resource '{resource.Name}' cannot be null or empty.");
        }

        // Get or create state for this command
        var state = _commandStates.GetOrAdd(command, _ => new CommandValidationState());

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // If validation already failed for this command, just log and return (allow resource to attempt start)
            if (state.ErrorMessage is not null)
            {
                _logger.LogWarning("Resource '{ResourceName}' may fail to start: {Message}", resource.Name, state.ErrorMessage);
                return;
            }

            // Check if already validated successfully
            if (state.ResolvedPath is not null)
            {
                _logger.LogDebug("Required command '{Command}' for resource '{ResourceName}' already validated, resolved to '{ResolvedPath}'.", command, resource.Name, state.ResolvedPath);
                return;
            }

            // Perform validation
            var resolved = PathLookupHelper.FindFullPathFromPath(command);
            var isValid = true;
            string? validationMessage = null;

            if (resolved is not null && annotation.ValidationCallback is not null)
            {
                (isValid, validationMessage) = await annotation.ValidationCallback(resolved, cancellationToken).ConfigureAwait(false);
            }

            if (resolved is null || !isValid)
            {
                var link = annotation.HelpLink;

                // Build the message for logging and exceptions (includes inline link if available)
                var message = (link, validationMessage) switch
                {
                    (null, not null) => validationMessage,
                    (not null, not null) => string.Format(CultureInfo.CurrentCulture, "Command '{0}' validation failed: {1}. For installation instructions, see: {2}", command, validationMessage, link),
                    (not null, null) => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location. For installation instructions, see: {1}", command, link),
                    _ => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location.", command)
                };

                // Build a simpler message for notifications (link is provided separately via options)
                var notificationMessage = (link, validationMessage) switch
                {
                    (null, not null) => validationMessage,
                    (not null, not null) => string.Format(CultureInfo.CurrentCulture, "Command '{0}' validation failed: {1}", command, validationMessage),
                    (not null, null) => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location.", command),
                    _ => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location.", command)
                };

                state.ErrorMessage = message;
                _logger.LogWarning("Resource '{ResourceName}' may fail to start: {Message}", resource.Name, message);

                // Show notification using interaction service if available (only once per command)
                if (_interactionService.IsAvailable)
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

                        _ = _interactionService.PromptNotificationAsync(
                            title: "Missing command",
                            message: notificationMessage,
                            options,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to show missing command notification");
                    }
                }

                // Don't throw - allow the resource to attempt to start (it will likely fail with a more specific error)
                return;
            }

            // Cache successful resolution
            state.ResolvedPath = resolved;
            _logger.LogDebug("Required command '{Command}' for resource '{ResourceName}' resolved to '{ResolvedPath}'.", command, resource.Name, resolved);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <summary>
    /// Tracks validation state for a single command to enable coalescing of notifications.
    /// </summary>
    private sealed class CommandValidationState
    {
        /// <summary>
        /// Synchronization gate to ensure only one validation runs at a time per command.
        /// </summary>
        public SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>
        /// The error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The resolved path if validation succeeded.
        /// </summary>
        public string? ResolvedPath { get; set; }
    }
}
#pragma warning restore ASPIREINTERACTION001
