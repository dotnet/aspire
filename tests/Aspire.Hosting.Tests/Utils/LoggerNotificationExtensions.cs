// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Necessary when LoggerNotificationExtensions is included directly in some projects
using ResourceNotificationService = Aspire.Hosting.ApplicationModel.ResourceNotificationService;
using ResourceLoggerService = Aspire.Hosting.ApplicationModel.ResourceLoggerService;

namespace Aspire.Hosting.Tests.Utils;

public static class LoggerNotificationExtensions
{
    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logText">The text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static Task WaitForTextAsync(this DistributedApplication app, string logText, string? resourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(logText);

        return WaitForTextAsync(app, (log) => log.Contains(logText), resourceName, cancellationToken);
    }

    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logTexts">Any text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static Task WaitForTextAsync(this DistributedApplication app, IEnumerable<string> logTexts, string? resourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(logTexts);

        return app.WaitForTextAsync((log) => logTexts.Any(x => log.Contains(x)), resourceName, cancellationToken);
    }

    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="predicate">A predicate checking the text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static Task WaitForTextAsync(this DistributedApplication app, Predicate<string> predicate, string? resourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(predicate);

        var hostApplicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        var watchCts = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping, cancellationToken);

        var tcs = new TaskCompletionSource();

        _ = Task.Run(() => WatchNotifications(app, resourceName, predicate, tcs, watchCts), watchCts.Token);

        return tcs.Task;
    }

    private static async Task WatchNotifications(DistributedApplication app, string? resourceName, Predicate<string> predicate, TaskCompletionSource tcs, CancellationTokenSource cancellationTokenSource)
    {
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LoggerNotificationExtensions));

        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        try
        {
            await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                if (resourceName != null && !string.Equals(resourceEvent.Resource.Name, resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var resourceId = resourceEvent.ResourceId;

                if (loggingResourceIds.Add(resourceId))
                {
                    // Start watching the logs for this resource ID
                    logWatchTasks.Add(WatchResourceLogs(tcs, resourceId, predicate, resourceLoggerService, cancellationTokenSource));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if the application stops prematurely or the text was detected.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while watching for resource notifications.");
        }
    }

    private static async Task WatchResourceLogs(TaskCompletionSource tcs, string resourceId, Predicate<string> predicate, ResourceLoggerService resourceLoggerService, CancellationTokenSource cancellationTokenSource)
    {
        await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId).WithCancellation(cancellationTokenSource.Token).ConfigureAwait(false))
        {
            foreach (var line in logEvent)
            {
                if (predicate(line.Content))
                {
                    tcs.SetResult();
                    cancellationTokenSource.Cancel();
                    return;
                }
            }
        }
    }
}
