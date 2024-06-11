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
    ILoggerFactory loggerFactory)
    : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // We need to pass the stopping token in here because the ResourceNotificationService doesn't stop on host shutdown
        await WatchNotifications(stoppingToken).ConfigureAwait(false);
    }

    private async Task WatchNotifications(CancellationToken cancellationToken)
    {
        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationToken).WithCancellation(cancellationToken))
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
        var logger = loggerFactory.CreateLogger($"ResourceLogs:{resource.Name}");
        await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId).WithCancellation(cancellationToken))
        {
            foreach (var line in logEvent)
            {
                var logLevel = line.IsErrorMessage ? LogLevel.Error : LogLevel.Information;
                logger.Log(logLevel, "Resource '{ResourceName}' logged line: {LineNumber} {LineContent}", resource.Name, line.LineNumber, line.Content);
            }
        }
    }
}
