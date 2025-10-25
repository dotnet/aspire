// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui.Utilities;

/// <summary>
/// Annotation that enables Android environment variable support via MSBuild targets file.
/// </summary>
/// <remarks>
/// Android MAUI applications cannot receive environment variables directly through the process environment
/// when launched via `dotnet run`. Instead, environment variables must be passed through MSBuild properties.
/// This annotation marks a resource for processing by <see cref="MauiAndroidEnvironmentSubscriber"/>.
/// </remarks>
internal sealed class MauiAndroidEnvironmentAnnotation : IResourceAnnotation
{
    // Marker annotation - actual logic is in the eventing subscriber
}

/// <summary>
/// Internal annotation to track that environment variables have been processed for a resource.
/// </summary>
internal sealed class MauiAndroidEnvironmentProcessedAnnotation : IResourceAnnotation
{
    public string TargetsFilePath { get; }

    public MauiAndroidEnvironmentProcessedAnnotation(string targetsFilePath)
    {
        TargetsFilePath = targetsFilePath;
    }
}

/// <summary>
/// Event subscriber that processes <see cref="MauiAndroidEnvironmentAnnotation"/> annotations.
/// </summary>
internal sealed class MauiAndroidEnvironmentSubscriber(
    DistributedApplicationExecutionContext executionContext,
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService) : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext execContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeResourceStartedEvent>(OnBeforeResourceStartedAsync);
        return Task.CompletedTask;
    }

    private async Task OnBeforeResourceStartedAsync(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        var resource = @event.Resource;

        // Only process Android resources with the environment annotation
        if (resource is not (MauiAndroidDeviceResource or MauiAndroidEmulatorResource))
        {
            return;
        }

        if (!resource.TryGetLastAnnotation<MauiAndroidEnvironmentAnnotation>(out _))
        {
            return;
        }

        var logger = loggerService.GetLogger(resource);

        // Check if we've already added the callback
        if (resource.TryGetLastAnnotation<MauiAndroidEnvironmentProcessedAnnotation>(out _))
        {
            // Already processed - callback is already registered
            return;
        }

        try
        {
            // Add a CommandLineArgsCallback that will generate the targets file
            // This runs AFTER all environment callbacks have been processed
            // The callback itself ensures idempotency by only generating the file once
            string? generatedFilePath = null;

            resource.Annotations.Add(new CommandLineArgsCallbackAnnotation(async context =>
            {
                // Only generate the file once, even if this callback is invoked multiple times
                if (generatedFilePath is null)
                {
                    generatedFilePath = await MauiEnvironmentHelper.CreateAndroidEnvironmentTargetsFileAsync(
                        resource,
                        executionContext,
                        logger,
                        cancellationToken
                    ).ConfigureAwait(false);

                    if (generatedFilePath is not null)
                    {
                        logger.LogInformation("Generated environment targets file for Android: {Path}", generatedFilePath);
                    }
                }

                if (generatedFilePath is not null)
                {
                    // Add the targets file as an MSBuild property via command-line argument
                    var commandLineArg = $"-p:CustomAfterMicrosoftCommonTargets={generatedFilePath}";
                    context.Args.Add(commandLineArg);
                }
            }));

            // Mark as processed to avoid duplicate callbacks
            resource.Annotations.Add(new MauiAndroidEnvironmentProcessedAnnotation(string.Empty));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure Android environment variables");

            // Report the error through the notification service
            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = new ResourceStateSnapshot("Failed to configure environment", KnownResourceStateStyles.Error)
            }).ConfigureAwait(false);

            throw;
        }
    }
}
