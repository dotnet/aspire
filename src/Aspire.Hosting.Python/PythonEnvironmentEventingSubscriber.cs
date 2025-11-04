// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Python;

/// <summary>
/// Subscribes to application events to validate Python environments before resources start.
/// This ensures that users are notified about missing virtual environments or uv installation.
/// </summary>
internal sealed class PythonEnvironmentEventingSubscriber : IDistributedApplicationEventingSubscriber
{
    public async Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        // Subscribe to BeforeResourceStartedEvent for all resources
        eventing.Subscribe<BeforeResourceStartedEvent>(OnBeforeResourceStartedAsync);
        
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task OnBeforeResourceStartedAsync(
        BeforeResourceStartedEvent @event,
        CancellationToken cancellationToken)
    {
        // Only process Python app resources
        if (@event.Resource is not PythonAppResource resource)
        {
            return;
        }
        
        // Get the validator from DI
        var validator = @event.Services.GetService<PythonEnvironmentValidator>();
        if (validator is null)
        {
            // Validator not registered, skip validation
            return;
        }

        try
        {
            // Validate the resource environment
            (bool isValid, string? validationMessage) = await validator.ValidateResourceAsync(
                resource,
                cancellationToken).ConfigureAwait(false);

            if (!isValid)
            {
                var logger = @event.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger<PythonEnvironmentEventingSubscriber>();
                
                logger.LogWarning(
                    "Python resource '{ResourceName}' validation failed: {ValidationMessage}",
                    resource.Name,
                    validationMessage);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the resource startup if validation fails
            var logger = @event.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger<PythonEnvironmentEventingSubscriber>();
            
            logger.LogDebug(
                ex,
                "Failed to validate Python resource '{ResourceName}'",
                resource.Name);
        }
    }
}
