// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.ConsoleLogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
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

    [Inject]
    public required DimensionManager DimensionManager { get; set; }

    [Inject]
    public required IOptions<DashboardOptions> Options { get; set; }

    internal LogEntries LogEntries { get; set; } = null!;

    public string? ResourceName { get; set; }

    protected override void OnInitialized()
    {
        LogEntries = new(Options.Value.Frontend.MaxConsoleLogCount);
    }

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
            DimensionManager.OnViewportInformationChanged += OnBrowserResize;
        }
    }

    private void OnBrowserResize(object? o, EventArgs args)
    {
        InvokeAsync(async () =>
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            await JS.InvokeVoidAsync("initializeContinuousScroll");
        });
    }

    internal async Task SetLogSourceAsync(string resourceName, IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> batches, bool convertTimestampsFromUtc = true)
    {
        ResourceName = resourceName;

        System.Diagnostics.Debug.Assert(LogEntries.GetEntries().Count == 0, "Expecting zero log entries");

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
                // Set the base line number using the reported line number of the first log line.
                if (LogEntries.EntriesCount == 0)
                {
                    LogEntries.BaseLineNumber = lineNumber;
                }

                var logEntry = logParser.CreateLogEntry(content, isErrorOutput);
                LogEntries.InsertSorted(logEntry);
            }

            StateHasChanged();
        }
    }

    private string GetDisplayTimestamp(DateTimeOffset timestamp)
    {
        var date = _convertTimestampsFromUtc ? TimeProvider.ToLocal(timestamp) : timestamp.DateTime;

        return date.ToString(KnownFormats.ConsoleLogsUITimestampFormat, CultureInfo.InvariantCulture);
    }

    internal async Task ClearLogsAsync()
    {
        await _cancellationSeries.ClearAsync();

        _applicationChanged = true;
        LogEntries.Clear();
        ResourceName = null;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationSeries.ClearAsync();
        DimensionManager.OnViewportInformationChanged -= OnBrowserResize;
    }
}
