// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
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
    private readonly bool _convertTimestampsFromUtc = true;
    private bool _logsCleared;

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    [Inject]
    public required ILogger<LogViewer> Logger { get; init; }

    [Inject]
    public required IOptions<DashboardOptions> Options { get; init; }

    internal LogEntries LogEntries { get; set; } = null!;

    public string? ResourceName { get; set; }

    protected override void OnInitialized()
    {
        LogEntries = new(Options.Value.Frontend.MaxConsoleLogCount);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_logsCleared)
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            _logsCleared = false;
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

    private string GetDisplayTimestamp(DateTimeOffset timestamp)
    {
        var date = _convertTimestampsFromUtc ? TimeProvider.ToLocal(timestamp) : timestamp.DateTime;

        return date.ToString(KnownFormats.ConsoleLogsUITimestampFormat, CultureInfo.InvariantCulture);
    }

    internal void ClearLogs()
    {
        Logger.LogDebug("Clearing logs for {ResourceName}.", ResourceName);

        _logsCleared = true;
        LogEntries.Clear();
        ResourceName = null;
        StateHasChanged();
    }

    public ValueTask DisposeAsync()
    {
        DimensionManager.OnViewportInformationChanged -= OnBrowserResize;
        return ValueTask.CompletedTask;
    }

    // Calling StateHasChanged on the page isn't updating the LogViewer.
    // This exposes way to tell the log view it has updated and to re-render.
    internal async Task LogsAddedAsync()
    {
        Logger.LogDebug("Logs added.");
        await InvokeAsync(StateHasChanged);
    }
}
