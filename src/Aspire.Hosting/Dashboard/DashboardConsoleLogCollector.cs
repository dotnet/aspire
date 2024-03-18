// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// The dashboard console log collector:
/// 1. Subscribes to resource updates.
/// 2. Watches for a running aspire-dashboard resource.
/// 3. Subscribes to console logs for the running aspire-dashboard resource.
/// 4. Writes console logs to the console.
/// </summary>
internal sealed partial class DashboardConsoleLogCollector : IHostedService
{
    private readonly ILogger<DashboardConsoleLogCollector> _logger;
    private readonly DashboardServiceData _dashboardServiceData;

    private readonly object _lock = new();
    private readonly CancellationTokenSource _collectorCts = new();
    private CancellationTokenSource? _resourceSubscriptionCts;
    private ResourceSnapshot? _dashboardResource;
    private Task? _logCollectionTask;

    public DashboardConsoleLogCollector(
        ILoggerFactory loggerFactory,
        DashboardServiceData dashboardServiceData)
    {
        _logger = loggerFactory.CreateLogger<DashboardConsoleLogCollector>();
        _dashboardServiceData = dashboardServiceData;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(_collectorCts.Cancel);

        _logCollectionTask = RunLogCollectionAsync(_collectorCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping dashboard console log collector.");

        lock (_lock)
        {
            _collectorCts.Cancel();
            _collectorCts.Dispose();
            _resourceSubscriptionCts?.Cancel();
        }

        try
        {
            if (_logCollectionTask is { } t)
            {
                await t.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping dashboard console log collector.");
        }
    }

    private async Task RunLogCollectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var resourceSubscription = _dashboardServiceData.SubscribeResources();
                foreach (var resource in resourceSubscription.InitialState)
                {
                    if (resource.DisplayName == KnownResourceNames.AspireDashboard)
                    {
                        ProcessDashboardResource(resource);
                    }
                    break;
                }

                var subscriptionTask = Task.Run(async () =>
                {
                    await foreach (var batch in resourceSubscription.Subscription.WithCancellation(cancellationToken))
                    {
                        foreach (var change in batch)
                        {
                            if (change.Resource.DisplayName == KnownResourceNames.AspireDashboard)
                            {
                                if (change.ChangeType == ResourceSnapshotChangeType.Upsert && change.Resource.State == ExecutableState.Running)
                                {
                                    ProcessDashboardResource(change.Resource);
                                }
                                else
                                {
                                    lock (_lock)
                                    {
                                        _dashboardResource = null;
                                        _resourceSubscriptionCts?.Cancel();
                                        _resourceSubscriptionCts = null;
                                    }
                                }
                            }
                        }
                    }
                }, cancellationToken);

                await subscriptionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error collecting dashboard logs. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task WatchLogs(ResourceSnapshot resourceSnapshot, CancellationToken cancellationToken)
    {
        const string DashboardConsolePrefix = "(Dashboard)";

        try
        {
            var consoleLogsSubscription = _dashboardServiceData.SubscribeConsoleLogs(resourceSnapshot.Name);
            if (consoleLogsSubscription == null)
            {
                throw new InvalidOperationException($"No subscription console logs returned for '{resourceSnapshot.Name}'.");
            }

            await foreach (var item in consoleLogsSubscription.WithCancellation(cancellationToken))
            {
                foreach (var log in item)
                {
                    var match = LogParsingConstants.Rfc3339RegEx.Match(log.Content);
                    var resolvedContent = match.Success ? log.Content.Substring(match.Length + 1) : log.Content;
                    var output = $"{DashboardConsolePrefix} {resolvedContent}";

                    if (!log.IsErrorMessage)
                    {
                        Console.WriteLine(output);
                    }
                    else
                    {
                        Console.Error.WriteLine(output);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching dashboard logs.");
        }
    }

    private void ProcessDashboardResource(ResourceSnapshot resource)
    {
        lock (_lock)
        {
            if (resource.Name != _dashboardResource?.Name)
            {
                _dashboardResource = resource;
                _resourceSubscriptionCts?.Cancel();
                _resourceSubscriptionCts = new();
                _ = WatchLogs(resource, _resourceSubscriptionCts.Token);
            }
        }
    }
}
