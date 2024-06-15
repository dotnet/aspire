// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Testing;

/// <summary>
/// A background service that aggregates resource logs and forwards them to the ILogger infrastructure.
/// </summary>
internal sealed class ResourceLogAggregatorBackgroundService(
    ResourceNotificationService resourceNotificationService,
    ResourceLoggerService resourceLoggerService,
    IHostEnvironment hostEnvironment,
    ILoggerFactory loggerFactory)
    : BackgroundService
{
    public Action<string>? OnLogStreamComplete { get; set; }

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // We need to pass the stopping token in here because the ResourceNotificationService doesn't stop on host shutdown
        return WatchNotifications(stoppingToken);
    }

    private async Task WatchNotifications(CancellationToken cancellationToken)
    {
        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
        {
            var resourceId = resourceEvent.ResourceId;

            if (loggingResourceIds.Add(resourceId))
            {
                // Start watching the logs for this resource ID
                logWatchTasks.Add(WatchResourceLogs(resourceEvent.Resource, resourceId, cancellationToken));
            }
        }

        await Task.WhenAll(logWatchTasks).ConfigureAwait(false);
    }

    private async Task WatchResourceLogs(IResource resource, string resourceId, CancellationToken cancellationToken)
    {
        var applicationName = hostEnvironment.ApplicationName;
        var logger = loggerFactory.CreateLogger($"{applicationName}.Resources.{resource.Name}");
        await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId).WithCancellation(cancellationToken))
        {
            foreach (var line in logEvent)
            {
                var logLevel = line.IsErrorMessage ? LogLevel.Error : LogLevel.Information;

                if (logger.IsEnabled(logLevel))
                {
                    // Log message format here approximates the format shown in the dashboard
                    logger.Log(logLevel, "{LineNumber}: {LineContent}", line.LineNumber, line.Content);
                }
            }
        }
        OnLogStreamComplete?.Invoke(resourceId);
    }
}
