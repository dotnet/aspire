// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;
public partial class LogViewer
{
    private readonly ConcurrentQueue<IEnumerable<LogEntry>> _preRenderQueue = new();
    private bool _renderComplete;
    private IJSObjectReference? _jsModule;

    internal async Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("clearLogs", cancellationToken);
        }
    }

    private ValueTask WriteLogsToDomAsync(IEnumerable<LogEntry> logs)
        => _jsModule is null ? ValueTask.CompletedTask : _jsModule.InvokeVoidAsync("addLogEntries", logs);

    internal async Task WatchLogsAsync(Func<IAsyncEnumerable<string[]>> watchMethod, LogEntryType logEntryType)
    {
        var logParser = new LogParser(logEntryType);

        await foreach (var logs in watchMethod())
        {
            var logEntries = new List<LogEntry>(logs.Length);
            foreach (var log in logs)
            {
                logEntries.Add(logParser.CreateLogEntry(log));
            }

            await WriteLogsAsync(logEntries);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "/_content/Aspire.Dashboard/Components/Controls/LogViewer.razor.js");

            while (_preRenderQueue.TryDequeue(out var logs))
            {
                await WriteLogsToDomAsync(logs);
            }
            _renderComplete = true;
        }
    }

    private async Task WriteLogsAsync(IEnumerable<LogEntry> logs)
    {
        if (_renderComplete)
        {
            await WriteLogsToDomAsync(logs);
        }
        else
        {
            _preRenderQueue.Enqueue(logs);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }
    }
}
