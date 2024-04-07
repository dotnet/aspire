// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

/// <summary>
/// A log viewing UI component that shows a live view of a log, with syntax highlighting and automatic scrolling.
/// </summary>
public sealed partial class LogViewer
{
    private readonly CancellationSeries _cancellationSeries = new();
    private bool _convertTimestampsFromUtc;
    private bool _applicationChanged;

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_applicationChanged)
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            _applicationChanged = false;
        }
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initializeContinuousScroll");
        }
    }

    private readonly List<LogEntry> _logEntries = new();
    private int? _baseLineNumber;

    internal async Task SetLogSourceAsync(IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> batches, bool convertTimestampsFromUtc)
    {
        _convertTimestampsFromUtc = convertTimestampsFromUtc;

        var cancellationToken = await _cancellationSeries.NextAsync();
        var logParser = new LogParser();

        // This needs to stay on the UI thread since we raise StateHasChanged() in the loop (hence the ConfigureAwait(true)).
        await foreach (var batch in batches.WithCancellation(cancellationToken).ConfigureAwait(true))
        {
            if (batch.Count is 0)
            {
                continue;
            }

            foreach (var (lineNumber, content, isErrorOutput) in batch)
            {
                // Keep track of the base line number to ensure that we can calculate the line number of each log entry.
                // This becomes important when the total number of log entries exceeds the limit and is truncated.
                if (_baseLineNumber is null)
                {
                    _baseLineNumber = lineNumber;
                }

                InsertSorted(_logEntries, logParser.CreateLogEntry(content, isErrorOutput));
            }

            StateHasChanged();
        }
    }

    private void InsertSorted(List<LogEntry> logEntries, LogEntry logEntry)
    {
        if (logEntry.ParentId != null)
        {
            // If we have a parent id, then we know we're on a non-timestamped line that is part
            // of a multi-line log entry. We need to find the prior line from that entry
            for (var rowIndex = logEntries.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var current = logEntries[rowIndex];

                if (current.Id == logEntry.ParentId && logEntry.LineIndex - 1 == current.LineIndex)
                {
                    InsertLogEntry(logEntries, rowIndex + 1, logEntry);
                    return;
                }
            }
        }
        else if (logEntry.Timestamp != null)
        {
            // Otherwise, if we have a timestamped line, we just need to find the prior line.
            // Since the rows are always in order, as soon as we see a timestamp
            // that is less than the one we're adding, we can insert it immediately after that
            for (var rowIndex = logEntries.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var current = logEntries[rowIndex];
                var currentTimestamp = current.Timestamp ?? current.ParentTimestamp;

                if (currentTimestamp != null && currentTimestamp < logEntry.Timestamp)
                {
                    InsertLogEntry(logEntries, rowIndex + 1, logEntry);
                    return;
                }
            }
        }

        // If we didn't find a place to insert then append it to the end. This happens with the first entry, but
        // could also happen if the logs don't have recognized timestamps.
        InsertLogEntry(logEntries, logEntries.Count, logEntry);

        void InsertLogEntry(List<LogEntry> logEntries, int index, LogEntry logEntry)
        {
            // Set the line number of the log entry.
            if (index == 0)
            {
                Debug.Assert(_baseLineNumber != null, "Should be set before this method is run.");
                logEntry.LineNumber = _baseLineNumber.Value;
            }
            else
            {
                logEntry.LineNumber = logEntries[index - 1].LineNumber + 1;
            }

            logEntries.Insert(index, logEntry);

            // If a log entry isn't inserted at the end then update the line numbers of all subsequent entries.
            for (var i = index + 1; i < logEntries.Count; i++)
            {
                logEntries[i].LineNumber++;
            }
        }
    }

    private string GetDisplayTimestamp(DateTimeOffset timestamp)
    {
        if (_convertTimestampsFromUtc)
        {
            timestamp = TimeProvider.ToLocal(timestamp);
        }

        return timestamp.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture);
    }

    internal async Task ClearLogsAsync()
    {
        await _cancellationSeries.ClearAsync();

        _applicationChanged = true;
        _logEntries.Clear();
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationSeries.ClearAsync();
    }
}
