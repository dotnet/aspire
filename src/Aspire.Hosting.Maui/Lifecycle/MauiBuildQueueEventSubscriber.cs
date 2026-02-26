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

        var semaphore = queueAnnotation.BuildSemaphore;

        // If the semaphore is already held, show "Queued" state while waiting.
        if (semaphore.CurrentCount == 0)
        {
            logger.LogInformation("Queued — waiting for another build of project '{ProjectName}' to complete.", parent.Name);

            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = s_queuedState
            }).ConfigureAwait(false);
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            logger.LogInformation("Building project '{ProjectName}' for {ResourceName}.", parent.Name, resource.Name);

            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = s_buildingState
            }).ConfigureAwait(false);

            // Run the Build target as a subprocess. Because we are inside
            // BeforeResourceStartedEvent the handler blocks DCP from starting the
            // process, so the "Building" state persists for the entire build duration.
            await RunBuildAsync(resource, logger, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
            logger.LogDebug("Released build lock (resource '{ResourceName}').", resource.Name);
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
}
