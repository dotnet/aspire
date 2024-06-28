// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private readonly LogEntries _logEntries = new();

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
                if (_logEntries.BaseLineNumber is null)
                {
                    _logEntries.BaseLineNumber = lineNumber;
                }

                _logEntries.InsertSorted(logParser.CreateLogEntry(content, isErrorOutput));
            }

            StateHasChanged();
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
