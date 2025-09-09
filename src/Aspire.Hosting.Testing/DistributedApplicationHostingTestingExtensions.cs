// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Extensions for working with <see cref="DistributedApplication"/> in test code.
/// </summary>
public static class DistributedApplicationHostingTestingExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resourceName of the resource.</param>
    /// <param name="endpointName">The resourceName of the endpoint on the resource to communicate with.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateHttpClient(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var baseUri = GetEndpointUriStringCore(app, resourceName, endpointName);
        var clientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient();
        client.BaseAddress = new(baseUri);

        return client;
    }

    /// <summary>
    /// Gets the connection string for the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The connection string for the specified resource.</returns>
    /// <exception cref="ArgumentException">The resource was not found or does not expose a connection string.</exception>
    public static ValueTask<string?> GetConnectionStringAsync(this DistributedApplication app, string resourceName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var resource = GetResource(app, resourceName);
        if (resource is not IResourceWithConnectionString resourceWithConnectionString)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ResourceDoesNotExposeConnectionStringExceptionMessage, resourceName), nameof(resourceName));
        }

        return resourceWithConnectionString.GetConnectionStringAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the endpoint for the specified resource.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="endpointName">The optional endpoint name. If none are specified, the single defined endpoint is returned.</param>
    /// <returns>A URI representation of the endpoint.</returns>
    /// <exception cref="ArgumentException">The resource was not found, no matching endpoint was found, or multiple endpoints were found.</exception>
    /// <exception cref="InvalidOperationException">The resource has no endpoints.</exception>
    public static Uri GetEndpoint(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        return new(GetEndpointUriStringCore(app, resourceName, endpointName));
    }

    static IResource GetResource(DistributedApplication app, string resourceName)
    {
        ThrowIfNotStarted(app);
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resources = applicationModel.Resources;
        var resource = resources.SingleOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ResourceNotFoundExceptionMessage, resourceName), nameof(resourceName));
        }

        return resource;
    }

    static string GetEndpointUriStringCore(DistributedApplication app, string resourceName, string? endpointName = default)
    {
        var resource = GetResource(app, resourceName);
        if (resource is not IResourceWithEndpoints resourceWithEndpoints)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ResourceHasNoAllocatedEndpointsExceptionMessage, resourceName), nameof(resourceName));
        }

        EndpointReference? endpoint;
        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = GetEndpointOrDefault(resourceWithEndpoints, endpointName);
        }
        else
        {
            endpoint = GetEndpointOrDefault(resourceWithEndpoints, "http") ?? GetEndpointOrDefault(resourceWithEndpoints, "https");
        }

        if (endpoint is null)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.EndpointForResourceNotFoundExceptionMessage, endpointName, resourceName), nameof(endpointName));
        }

        return endpoint.Url;
    }

    static void ThrowIfNotStarted(DistributedApplication app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        if (!lifetime.ApplicationStarted.IsCancellationRequested)
        {
            throw new InvalidOperationException(Properties.Resources.ApplicationNotStartedExceptionMessage);
        }
    }

    static EndpointReference? GetEndpointOrDefault(IResourceWithEndpoints resourceWithEndpoints, string endpointName)
    {
        var reference = resourceWithEndpoints.GetEndpoint(endpointName);

        return reference.IsAllocated ? reference : null;
    }

    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logText">The text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the specified text is logged.</returns>
    public static Task WaitForTextAsync(this DistributedApplication app, string logText, string? resourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(logText);

        return WaitForTextAsync(app, (log) => log.Contains(logText), resourceName, cancellationToken);
    }

    /// <summary>
    /// Waits for any of the specified texts to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logTexts">Any text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when any of the specified texts is logged.</returns>
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when text matching the predicate is logged.</returns>
    public static Task WaitForTextAsync(this DistributedApplication app, Predicate<string> predicate, CancellationToken cancellationToken = default)
        => app.WaitForTextAsync(predicate, resourceName: null, cancellationToken);

    /// <summary>
    /// Waits for the specified text to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="predicate">A predicate checking the text to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when text matching the predicate is logged.</returns>
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

    /// <summary>
    /// Waits for all the specified texts to be logged.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication" /> instance to watch.</param>
    /// <param name="logTexts">All texts to wait for.</param>
    /// <param name="resourceName">An optional resource name to filter the logs for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when all of the specified texts have been logged.</returns>
    public static async Task WaitForAllTextAsync(this DistributedApplication app, IEnumerable<string> logTexts, string? resourceName = null, CancellationToken cancellationToken = default)
    {
        var table = logTexts.ToList();
        try
        {
            await app.WaitForTextAsync((log) =>
            {
                foreach (var text in table)
                {
                    if (log.Contains(text))
                    {
                        table.Remove(text);
                        break;
                    }
                }

                return table.Count == 0;
            }, resourceName, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException te) when (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException($"Task was canceled before these messages were found: '{string.Join("', '", table)}'", te);
        }
    }

    private static async Task WatchNotifications(DistributedApplication app, string? resourceName, Predicate<string> predicate, TaskCompletionSource tcs, CancellationTokenSource cancellationTokenSource)
    {
        var resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DistributedApplicationHostingTestingExtensions));

        var loggingResourceIds = new HashSet<string>();
        var logWatchTasks = new List<Task>();

        try
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(cancellationTokenSource.Token).ConfigureAwait(false))
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
            tcs.TrySetCanceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while watching for resource notifications.");
            tcs.TrySetException(ex);
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
