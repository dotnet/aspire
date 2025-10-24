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

        // Check if we've already processed this resource
        if (resource.TryGetLastAnnotation<MauiAndroidEnvironmentProcessedAnnotation>(out var processed))
        {
            logger.LogDebug("Android environment variables already processed, reusing targets file: {Path}", processed.TargetsFilePath);
            return;
        }

        try
        {
            // Generate the targets file
            var targetsFilePath = await MauiEnvironmentHelper.CreateAndroidEnvironmentTargetsFileAsync(
                resource,
                executionContext,
                logger,
                cancellationToken
            ).ConfigureAwait(false);

            if (targetsFilePath is null)
            {
                // No environment variables to process
                return;
            }

            logger.LogInformation("Generated environment targets file for Android: {Path}", targetsFilePath);

            // Add the targets file as an MSBuild property via command-line argument
            // The -p:CustomAfterMicrosoftCommonTargets property tells MSBuild to import this file after common targets
            // Note: No quotes around the path - MSBuild handles paths with spaces internally
            var commandLineArg = $"-p:CustomAfterMicrosoftCommonTargets={targetsFilePath}";

            // Add the argument to the resource
            // Note: We use CommandLineArgsCallbackAnnotation to ensure this runs after other configuration
            resource.Annotations.Add(new CommandLineArgsCallbackAnnotation(async context =>
            {
                context.Args.Add(commandLineArg);
                await Task.CompletedTask.ConfigureAwait(false);
            }));

            // Mark as processed to avoid duplicate processing
            resource.Annotations.Add(new MauiAndroidEnvironmentProcessedAnnotation(targetsFilePath));

            logger.LogDebug("Added MSBuild argument: {Arg}", commandLineArg);
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
