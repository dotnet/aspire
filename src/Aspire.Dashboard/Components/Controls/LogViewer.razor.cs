// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
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
    private readonly TaskCompletionSource _whenDomReady = new();
    private readonly CancellationSeries _cancellationSeries = new();
    private IJSObjectReference? _jsModule;

    [Inject]
    public required TimeProvider TimeProvider { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Controls/LogViewer.razor.js");

            _whenDomReady.TrySetResult();
        }
    }

    internal async Task SetLogSourceAsync(IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> batches, bool convertTimestampsFromUtc)
    {
        var cancellationToken = await _cancellationSeries.NextAsync();
        var logParser = new LogParser(TimeProvider, convertTimestampsFromUtc);

        // Ensure we are able to write to the DOM.
        await _whenDomReady.Task;

        await foreach (var batch in batches.WithCancellation(cancellationToken))
        {
            if (batch.Count is 0)
            {
                continue;
            }

            List<LogEntry> entries = new(batch.Count);

            foreach (var (content, isErrorOutput) in batch)
            {
                entries.Add(logParser.CreateLogEntry(content, isErrorOutput));
            }

            await _jsModule!.InvokeVoidAsync("addLogEntries", cancellationToken, entries);
        }
    }

    internal async Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        await _cancellationSeries.ClearAsync();

        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("clearLogs", cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _whenDomReady.TrySetCanceled();

        await _cancellationSeries.ClearAsync();

        await JSInteropHelpers.SafeDisposeAsync(_jsModule);
    }
}
