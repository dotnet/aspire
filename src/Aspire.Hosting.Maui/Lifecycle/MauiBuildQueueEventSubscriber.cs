// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui.Annotations;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui.Lifecycle;

/// <summary>
/// Event subscriber that serializes MAUI platform resource builds per-project.
/// </summary>
/// <remarks>
/// Multiple MAUI platform resources (Android, iOS, Mac Catalyst, Windows) can reference
/// the same project. MSBuild cannot handle concurrent builds of the same project file,
/// so this subscriber uses a semaphore to ensure only one platform builds at a time.
/// Resources waiting for their turn show a "Queued" state in the dashboard.
/// The build is run as a separate <c>dotnet build</c> subprocess so that the exit code
/// provides reliable build-completion detection and the "Building" state persists in the
/// dashboard for the full build duration. Once the build completes, DCP launches the app
/// with just the Run target.
/// </remarks>
internal class MauiBuildQueueEventSubscriber(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationEventingSubscriber
{
    private static readonly ResourceStateSnapshot s_queuedState = new("Queued", KnownResourceStateStyles.Info);
    private static readonly ResourceStateSnapshot s_buildingState = new("Building", KnownResourceStateStyles.Info);

    /// <summary>
    /// Maximum time to wait for a <c>dotnet build</c> process before cancelling.
    /// Prevents a hung build from blocking the queue indefinitely.
    /// </summary>
    internal TimeSpan BuildTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <inheritdoc/>
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeResourceStartedEvent>(OnBeforeResourceStartedAsync);
        return Task.CompletedTask;
    }

    private async Task OnBeforeResourceStartedAsync(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Resource is not IMauiPlatformResource mauiResource)
        {
            return;
        }

        var resource = @event.Resource;
        var parent = mauiResource.Parent;
        var logger = loggerService.GetLogger(resource);

        if (!parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var queueAnnotation))
        {
            return;
        }

        // Replace the default stop command with one that can cancel queued/building resources.
        // This must happen here (not at app model build time) because the default lifecycle
        // commands are added by DcpExecutor.EnsureRequiredAnnotations AFTER app model building.
        EnsureStopCommandReplaced(resource, queueAnnotation);

        var semaphore = queueAnnotation.BuildSemaphore;

        // Create a per-resource CTS so the stop command can cancel a queued/building resource.
        using var resourceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        queueAnnotation.ResourceCancellations[resource.Name] = resourceCts;

        var semaphoreAcquired = false;

        try
        {
            // If the semaphore is already held, show "Queued" state while waiting.
            if (semaphore.CurrentCount == 0)
            {
                logger.LogInformation("Queued — waiting for another build of project '{ProjectName}' to complete.", parent.Name);

                await notificationService.PublishUpdateAsync(resource, s => s with
                {
                    State = s_queuedState
                }).ConfigureAwait(false);
            }

            await semaphore.WaitAsync(resourceCts.Token).ConfigureAwait(false);
            semaphoreAcquired = true;

            logger.LogInformation("Building project '{ProjectName}' for {ResourceName}.", parent.Name, resource.Name);

            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = s_buildingState
            }).ConfigureAwait(false);

            await RunBuildAsync(resource, logger, resourceCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (resourceCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // The per-resource CTS was cancelled by CancelResource (user clicked stop).
            // Set a terminal state so the resource shows as stopped in the dashboard,
            // and DON'T re-throw — the orchestrator's cancellation token is still valid,
            // so we don't want to signal a failure that could affect other resources.
            logger.LogInformation("Build cancelled for resource '{ResourceName}'.", resource.Name);

            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = new ResourceStateSnapshot("Exited", KnownResourceStateStyles.Info),
                ExitCode = -1,
                StopTimeStamp = DateTime.UtcNow,
            }).ConfigureAwait(false);
        }
        finally
        {
            if (semaphoreAcquired)
            {
                semaphore.Release();
                logger.LogDebug("Released build lock (resource '{ResourceName}').", resource.Name);
            }

            queueAnnotation.ResourceCancellations.TryRemove(resource.Name, out _);
        }
    }

    /// <summary>
    /// Runs <c>dotnet build</c> as a subprocess and pipes its output to the resource logger.
    /// </summary>
    internal virtual async Task RunBuildAsync(IResource resource, ILogger logger, CancellationToken cancellationToken)
    {
        if (!resource.TryGetLastAnnotation<MauiBuildInfoAnnotation>(out var buildInfo))
        {
            logger.LogWarning("No build info annotation found for resource '{ResourceName}'. Skipping build.", resource.Name);
            return;
        }

        var args = new List<string> { "build", buildInfo.ProjectPath };

        if (!string.IsNullOrEmpty(buildInfo.TargetFramework))
        {
            args.Add("-f");
            args.Add(buildInfo.TargetFramework);
        }

        if (!string.IsNullOrEmpty(buildInfo.Configuration))
        {
            args.Add("--configuration");
            args.Add(buildInfo.Configuration);
        }

        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = buildInfo.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        logger.LogInformation("Running: dotnet {Arguments}", string.Join(" ", args));

        // Apply a timeout so that a hung build does not block the queue forever.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(BuildTimeout);
        var token = timeoutCts.Token;

        using var process = new Process { StartInfo = psi };

        process.Start();

        // Pipe stdout/stderr to the resource logger so output is visible in the dashboard.
        var stdoutTask = PipeOutputAsync(process.StandardOutput, logger, LogLevel.Information, token);
        var stderrTask = PipeOutputAsync(process.StandardError, logger, LogLevel.Warning, token);

        try
        {
            await process.WaitForExitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process, logger);
            throw;
        }
        finally
        {
            // Always drain remaining output — even on cancellation the process was killed
            // and the streams will reach EOF, so the tasks will complete promptly.
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Build failed for resource '{resource.Name}' with exit code {process.ExitCode}.");
        }

        logger.LogInformation("Build succeeded for resource '{ResourceName}'.", resource.Name);
    }

    private static async Task PipeOutputAsync(System.IO.StreamReader reader, ILogger logger, LogLevel level, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                logger.Log(level, "{Line}", line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the build is cancelled or timed out.
        }
    }

    private static void TryKillProcess(Process process, ILogger logger)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to kill build process.");
        }
    }

    /// <summary>
    /// Replaces the default stop command with one that can cancel queued/building resources
    /// via the <see cref="MauiBuildQueueAnnotation.CancelResource"/> method, while delegating
    /// to the original stop command for the Running state.
    /// </summary>
    private static void EnsureStopCommandReplaced(IResource resource, MauiBuildQueueAnnotation queueAnnotation)
    {
        // Only replace once per resource (supports restart).
        if (resource.Annotations.OfType<MauiStopCommandReplacedAnnotation>().Any())
        {
            return;
        }

        resource.Annotations.Add(new MauiStopCommandReplacedAnnotation());

        var originalStop = resource.Annotations
            .OfType<ResourceCommandAnnotation>()
            .SingleOrDefault(a => a.Name == KnownResourceCommands.StopCommand);

        if (originalStop is null)
        {
            return;
        }

        // Remove the original and add our replacement.
        resource.Annotations.Remove(originalStop);

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.StopCommand,
            displayName: "Stop",
            updateState: context =>
            {
                var state = context.ResourceSnapshot.State?.Text;

                // Show stop for Queued/Building states.
                if (state is "Queued" or "Building")
                {
                    return ResourceCommandState.Enabled;
                }

                // For all other states, delegate to original logic.
                return originalStop.UpdateState(context);
            },
            executeCommand: async context =>
            {
                // Cancel via the annotation — works for both Queued and Building.
                var wasCancelled = queueAnnotation.CancelResource(context.ResourceName);

                if (wasCancelled)
                {
                    return CommandResults.Success();
                }

                // Resource is past the queue (Running) — delegate to original stop.
                return await originalStop.ExecuteCommand(context).ConfigureAwait(false);
            },
            displayDescription: null,
            parameter: null,
            confirmationMessage: null,
            iconName: "Stop",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));
    }

    /// <summary>
    /// Marker annotation to prevent replacing the stop command more than once.
    /// </summary>
    private sealed class MauiStopCommandReplacedAnnotation : IResourceAnnotation;
}
