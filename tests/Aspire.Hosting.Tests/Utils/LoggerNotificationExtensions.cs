// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using IResource = Aspire.Hosting.ApplicationModel.IResource;
using ResourceLoggerService = Aspire.Hosting.ApplicationModel.ResourceLoggerService;
using ResourceNotificationService = Aspire.Hosting.ApplicationModel.ResourceNotificationService;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Tests.Utils;

internal static class LoggerNotificationExtensions
{
    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logText">The text to wait for.</param>
    /// <param name="resource">An optional <see cref="IResource"/> instance to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static Task WaitForText(this DistributedApplication app, string logText, IResource? resource = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(logText);

        return WaitForText(app, [logText], resource, cancellationToken);
    }

    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logTexts">Any text to wait for.</param>
    /// <param name="resource">An optional <see cref="IResource"/> instance to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static Task WaitForText(this DistributedApplication app, IEnumerable<string> logTexts, IResource? resource = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(logTexts);

        var hostApplicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping, cancellationToken);
        var watchToken = watchCts.Token;

        var tcs = new TaskCompletionSource();

        _ = Task.Run(() => WatchNotifications(app, resource, logTexts, tcs, watchToken), watchToken);

        return tcs.Task;
    }

    private static async Task WatchNotifications(DistributedApplication app, IResource? resource, IEnumerable<string> logTexts, TaskCompletionSource tcs, CancellationToken cancellationToken)
    {
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();

        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
        {
            if (resource != null && resourceEvent.Resource != resource)
            {
                continue;
            }

            var resourceId = resourceEvent.ResourceId;

            if (loggingResourceIds.Add(resourceId))
            {
                // Start watching the logs for this resource ID
                logWatchTasks.Add(WatchResourceLogs(tcs, resourceId, logTexts, resourceLoggerService, cancellationToken));
            }
        }

        await Task.WhenAny(logWatchTasks).ConfigureAwait(false);
    }

    private static async Task WatchResourceLogs(TaskCompletionSource tcs, string resourceId, IEnumerable<string> logTexts, ResourceLoggerService resourceLoggerService, CancellationToken cancellationToken)
    {
        await foreach (var logEvent in resourceLoggerService.WatchAsync(resourceId).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var line in logEvent)
            {
                foreach (var log in logTexts)
                {
                    if (line.Content.Contains(log))
                    {
                        tcs.SetResult();
                        return;
                    }
                }
            }
        }
    }
}
