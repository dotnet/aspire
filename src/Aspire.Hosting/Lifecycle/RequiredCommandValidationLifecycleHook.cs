// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
/// </remarks>
internal sealed class RequiredCommandValidationLifecycleHook(
    ILogger<RequiredCommandValidationLifecycleHook> logger) : IDistributedApplicationEventingSubscriber
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        var resolved = CommandResolver.ResolveCommand(command);
        var isValid = true;
        string? validationMessage = null;

        if (resolved is not null && annotation.ValidationCallback is not null)
        {
            (isValid, validationMessage) = await annotation.ValidationCallback(resolved, cancellationToken).ConfigureAwait(false);
        }

        if (resolved is null || !isValid)
        {
            var link = annotation.HelpLink;
            var message = (link, validationMessage) switch
            {
                (null, not null) => validationMessage,
                (not null, not null) => string.Format(CultureInfo.CurrentCulture, "Command '{0}' validation failed: {1}. For installation instructions, see: {2}", command, validationMessage, link),
                (not null, null) => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location. For installation instructions, see: {1}", command, link),
                _ => string.Format(CultureInfo.CurrentCulture, "Required command '{0}' was not found on PATH or at the specified location.", command)
            };

            _logger.LogError("Resource '{ResourceName}' cannot start: {Message}", resource.Name, message);
            throw new DistributedApplicationException(message);
        }

        _logger.LogDebug("Required command '{Command}' for resource '{ResourceName}' resolved to '{ResolvedPath}'.", command, resource.Name, resolved);
    }
}
