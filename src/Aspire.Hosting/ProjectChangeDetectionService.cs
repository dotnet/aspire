// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Background service that periodically checks project resources for source file changes
/// and notifies users when a rebuild is needed.
/// </summary>
internal sealed class ProjectChangeDetectionService(
    ILogger<ProjectChangeDetectionService> logger,
    ResourceNotificationService resourceNotificationService,
    ResourceLoggerService resourceLoggerService,
    IInteractionService interactionService,
    IConfiguration configuration) : BackgroundService
{
    private readonly Dictionary<string, ProjectMonitorState> _monitoredProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(
        int.TryParse(configuration["ASPIRE_PROJECT_CHANGE_DETECTION_INTERVAL"], out var interval) ? interval : 10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run if explicitly enabled.
        if (configuration.GetBool("ASPIRE_PROJECT_CHANGE_DETECTION") is not true)
        {
            logger.LogDebug("Project change detection is disabled. Set ASPIRE_PROJECT_CHANGE_DETECTION=true to enable.");
            return;
        }

        logger.LogInformation("Project change detection enabled with {Interval}s check interval.", _checkInterval.TotalSeconds);

        try
        {
            var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

            await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
            {
                if (resourceEvent.Resource is not ProjectResource projectResource)
                {
                    continue;
                }

                var resourceName = resourceEvent.Resource.Name;
                var stateText = resourceEvent.Snapshot.State?.Text;

                if (stateText == KnownResourceStates.Running)
                {
                    if (!_monitoredProjects.ContainsKey(resourceName))
                    {
                        // The project just entered Running state — start monitoring it.
                        await StartMonitoringAsync(resourceName, projectResource, stoppingToken).ConfigureAwait(false);
                    }
                }
                else if (KnownResourceStates.TerminalStates.Contains(stateText) ||
                         stateText == KnownResourceStates.Stopping ||
                         stateText is "Building")
                {
                    // The project stopped or is being rebuilt — stop monitoring.
                    StopMonitoring(resourceName);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown.
        }
    }

    private async Task StartMonitoringAsync(string resourceName, ProjectResource projectResource, CancellationToken stoppingToken)
    {
        if (!projectResource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            return;
        }

        var projectPath = metadata.ProjectPath;
        var resourceLogger = resourceLoggerService.GetLogger(projectResource);

        logger.LogDebug("Capturing file closure for project '{ResourceName}' at {ProjectPath}", resourceName, projectPath);

        var closure = await ProjectBuildHelper.GetProjectFileClosureAsync(projectPath, logger, stoppingToken).ConfigureAwait(false);
        if (closure is null)
        {
            logger.LogDebug("Could not capture file closure for project '{ResourceName}'. Change detection will not be active for this resource.", resourceName);
            return;
        }

        var monitorState = new ProjectMonitorState(closure, resourceLogger);
        _monitoredProjects[resourceName] = monitorState;

        logger.LogDebug("Started monitoring {FileCount} files for project '{ResourceName}'", closure.FileTimestamps.Count, resourceName);

        // Start a background task to periodically check for changes.
        _ = Task.Run(async () =>
        {
            try
            {
                await MonitorProjectAsync(resourceName, monitorState, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error monitoring project '{ResourceName}'", resourceName);
            }
        }, stoppingToken);
    }

    private async Task MonitorProjectAsync(string resourceName, ProjectMonitorState monitorState, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !monitorState.IsCancelled)
        {
            await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);

            if (monitorState.IsCancelled)
            {
                break;
            }

            var changedFiles = monitorState.Closure.GetChangedFiles();
            if (changedFiles.Count == 0)
            {
                continue;
            }

            // Debounce: wait a bit to let the user finish saving files.
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);

            if (monitorState.IsCancelled)
            {
                break;
            }

            // Re-check after debounce.
            changedFiles = monitorState.Closure.GetChangedFiles();
            if (changedFiles.Count == 0)
            {
                continue;
            }

            // Don't notify again if we already notified and the user hasn't rebuilt.
            if (monitorState.HasNotified)
            {
                continue;
            }

            monitorState.HasNotified = true;
            var fileList = changedFiles.Count <= 3
                ? string.Join(", ", changedFiles.Select(Path.GetFileName))
                : $"{string.Join(", ", changedFiles.Take(3).Select(Path.GetFileName))} and {changedFiles.Count - 3} more";

            monitorState.ResourceLogger.LogInformation(
                "[change-detection] Source files changed: {Files}. Use the Rebuild command to apply changes.",
                fileList);

            logger.LogDebug("Detected {Count} changed files for project '{ResourceName}': {Files}",
                changedFiles.Count, resourceName, fileList);

            // Show a notification in the dashboard if available.
            if (interactionService.IsAvailable)
            {
                logger.LogDebug("Sending notification for project '{ResourceName}' source changes.", resourceName);
                _ = interactionService.PromptNotificationAsync(
                    title: $"Source changes detected in '{resourceName}'",
                    message: $"Source files have changed ({fileList}). Use the Rebuild command to apply the changes.",
                    options: new NotificationInteractionOptions
                    {
                        Intent = MessageIntent.Information,
                    },
                    cancellationToken: stoppingToken);
            }
            else
            {
                logger.LogDebug("Interaction service is not available; skipping notification for project '{ResourceName}'.", resourceName);
            }
        }
    }

    private void StopMonitoring(string resourceName)
    {
        if (_monitoredProjects.TryGetValue(resourceName, out var state))
        {
            state.IsCancelled = true;
            _monitoredProjects.Remove(resourceName);
            logger.LogDebug("Stopped monitoring project '{ResourceName}'", resourceName);
        }
    }

    private sealed class ProjectMonitorState(ProjectFileClosure closure, ILogger resourceLogger)
    {
        public ProjectFileClosure Closure { get; } = closure;
        public ILogger ResourceLogger { get; } = resourceLogger;
        public bool HasNotified { get; set; }
        public bool IsCancelled { get; set; }
    }
}
