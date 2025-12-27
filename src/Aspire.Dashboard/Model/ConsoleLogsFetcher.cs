// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Hosting.ConsoleLogs;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Fetches console logs from the dashboard client and converts them to log entries.
/// </summary>
public sealed class ConsoleLogsFetcher
{
    private readonly IDashboardClient _dashboardClient;
    private readonly ConsoleLogsManager _consoleLogsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogsFetcher"/> class.
    /// </summary>
    /// <param name="dashboardClient">The dashboard client.</param>
    /// <param name="consoleLogsManager">The console logs manager.</param>
    public ConsoleLogsFetcher(IDashboardClient dashboardClient, ConsoleLogsManager consoleLogsManager)
    {
        _dashboardClient = dashboardClient;
        _consoleLogsManager = consoleLogsManager;
    }

    /// <summary>
    /// Fetches console logs for a resource and converts them to log entries.
    /// </summary>
    /// <param name="resourceName">The name of the resource to fetch logs for.</param>
    /// <param name="filterDate">Optional filter date. Logs with timestamps at or before this date are excluded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of log entries for the resource.</returns>
    public async Task<List<LogEntry>> FetchLogEntriesAsync(string resourceName, DateTime? filterDate, CancellationToken cancellationToken)
    {
        var logEntries = new List<LogEntry>();
        var logParser = new LogParser(ConsoleColor.Black);

        await foreach (var batch in _dashboardClient.GetConsoleLogs(resourceName, cancellationToken).ConfigureAwait(false))
        {
            foreach (var logLine in batch)
            {
                var logEntry = logParser.CreateLogEntry(logLine.Content, logLine.IsErrorMessage, resourcePrefix: null);

                // Apply filter if specified
                if (filterDate is not null && logEntry.Timestamp is not null && logEntry.Timestamp <= filterDate)
                {
                    continue;
                }

                logEntries.Add(logEntry);
            }
        }

        return logEntries;
    }

    /// <summary>
    /// Fetches console logs for all resources and converts them to log entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping resource names to their log entries.</returns>
    public async Task<Dictionary<string, List<LogEntry>>> FetchAllLogEntriesAsync(CancellationToken cancellationToken)
    {
        if (!_dashboardClient.IsEnabled)
        {
            return [];
        }

        var resources = _dashboardClient.GetResources();
        var result = new Dictionary<string, List<LogEntry>>(StringComparer.OrdinalIgnoreCase);

        // Fetch logs for all resources in parallel
        var logTasks = resources.Select(async resource =>
        {
            var filterDate = _consoleLogsManager.GetFilterDate(resource.Name);
            var logEntries = await FetchLogEntriesAsync(resource.Name, filterDate, cancellationToken).ConfigureAwait(false);
            var resourceName = ResourceViewModel.GetResourceName(resource, resources);
            return (ResourceName: resourceName, LogEntries: logEntries);
        });

        var results = await Task.WhenAll(logTasks).ConfigureAwait(false);

        foreach (var (resourceName, logEntries) in results)
        {
            if (logEntries.Count > 0)
            {
                result[resourceName] = logEntries;
            }
        }

        return result;
    }
}
