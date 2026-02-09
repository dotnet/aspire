// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Resources;
using Hex1b;
using Hex1b.Layout;
using Hex1b.Widgets;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.UI;

/// <summary>
/// Represents an AppHost entry in the drawer list.
/// </summary>
internal sealed class AppHostEntry
{
    public required string DisplayName { get; init; }
    public required string FullPath { get; init; }
    public required IAppHostAuxiliaryBackchannel Connection { get; init; }
}

/// <summary>
/// Main TUI for the aspire monitor command.
/// </summary>
internal sealed class AspireMonitorTui
{
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger _logger;

    // Mutable state — captured by closures in the widget builder
    private List<AppHostEntry> _appHosts = [];
    private int _selectedAppHostIndex;
    private readonly Dictionary<string, ResourceSnapshot> _resources = [];
    private bool _isConnecting;
    private string? _errorMessage;
    private bool _showSplash = true;
    private bool _isNavExpanded = true;
    private object? _focusedResourceKey;
    private object? _focusedParameterKey;
    private CancellationTokenSource? _watchCts;
    private Hex1bApp? _app;

    public AspireMonitorTui(IAuxiliaryBackchannelMonitor backchannelMonitor, ILogger logger)
    {
        _backchannelMonitor = backchannelMonitor;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

        _appHosts = _backchannelMonitor.Connections
            .Where(c => c.AppHostInfo is not null)
            .OrderByDescending(c => c.IsInScope)
            .Select(c => new AppHostEntry
            {
                DisplayName = ShortenPath(c.AppHostInfo!.AppHostPath ?? "Unknown"),
                FullPath = c.AppHostInfo!.AppHostPath ?? "Unknown",
                Connection = c
            })
            .ToList();

        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHex1bApp((app, options) =>
            {
                _app = app;
                options.EnableDefaultCtrlCExit = true;
                return BuildWidget;
            })
            .WithMouse()
            .Build();

        // After a brief splash, transition to the main screen and connect
        _ = Task.Run(async () =>
        {
            await Task.Delay(AspireMonitorSplash.SplashDurationMs, cancellationToken).ConfigureAwait(false);
            _showSplash = false;
            _app?.Invalidate();

            if (_appHosts.Count > 0)
            {
                await ConnectToAppHostAsync(0, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);

        await terminal.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private Hex1bWidget BuildWidget(RootContext ctx)
    {
        var content = _showSplash
            ? AspireMonitorSplash.Build(ctx)
            : BuildMainScreen(ctx);

        return ctx.ThemePanel(AspireTheme.Apply, content).Fill();
    }

    private Hex1bWidget BuildMainScreen(RootContext ctx)
    {
        var appHostItems = _appHosts.Select(a => a.DisplayName).ToArray();

        // Tab panel with Resources and Parameters tabs
        var tabPanel = ctx.TabPanel(tabs => [
            tabs.Tab(MonitorCommandStrings.ResourcesTab, c => BuildResourcesTab(c)),
            tabs.Tab(MonitorCommandStrings.ParametersTab, c => BuildParametersTab(c))
        ]).Fill();

        // Wrap tab panel in notification panel
        var mainContent = ctx.NotificationPanel(tabPanel).Fill();

        // Drawer content: list of AppHosts
        Hex1bWidget drawerBody = appHostItems.Length > 0
            ? ctx.List(appHostItems)
                .OnSelectionChanged(args =>
                {
                    if (args.SelectedIndex != _selectedAppHostIndex)
                    {
                        _selectedAppHostIndex = args.SelectedIndex;
                        _ = ConnectToAppHostAsync(_selectedAppHostIndex, CancellationToken.None);
                    }
                })
                .Fill()
            : ctx.Text(MonitorCommandStrings.NoRunningAppHostsFound);

        // Main layout: drawer on the left, content on the right
        var body = ctx.HStack(h => [
            h.Drawer()
                .Expanded(_isNavExpanded)
                .ExpandedContent(e => [
                    e.VStack(nav => [
                        nav.HStack(header => [
                            header.Text($" {MonitorCommandStrings.AppHostsDrawerTitle}"),
                            header.Text("").Fill(),
                            header.Button("«").OnClick(_ => { _isNavExpanded = false; })
                        ]).FixedHeight(1),
                        nav.Separator(),
                        ..BuildAppHostList(nav)
                    ])
                ])
                .CollapsedContent(c => [
                    c.Button("»").OnClick(_ => { _isNavExpanded = true; })
                ]),
            h.Border(mainContent, title: GetSelectedAppHostTitle()).Fill()
        ]).Fill();

        return ctx.VStack(outer => [
            body,
            outer.InfoBar(bar => [
                bar.Section("q: " + MonitorCommandStrings.QuitShortcut),
                bar.Separator(" │ "),
                bar.Section("Tab: " + MonitorCommandStrings.TabShortcut),
                bar.Separator(" │ "),
                bar.Section(GetStatusText()).FillWidth()
            ])
        ]).Fill();
    }

    private IEnumerable<Hex1bWidget> BuildAppHostList(WidgetContext<VStackWidget> nav)
    {
        if (_appHosts.Count == 0)
        {
            return [nav.Text($"  {MonitorCommandStrings.NoRunningAppHostsFound}")];
        }

        return _appHosts.Select((appHost, i) =>
            nav.Button(_selectedAppHostIndex == i
                ? $" ▸ {appHost.DisplayName}"
                : $"   {appHost.DisplayName}")
                .OnClick(e =>
                {
                    if (i != _selectedAppHostIndex)
                    {
                        _selectedAppHostIndex = i;
                        _ = ConnectToAppHostAsync(i, CancellationToken.None);
                    }
                })
        );
    }

    private IEnumerable<Hex1bWidget> BuildResourcesTab(WidgetContext<VStackWidget> ctx)
    {
        if (_isConnecting)
        {
            return [ctx.Text($"  {MonitorCommandStrings.ConnectingToAppHost}")];
        }

        if (_errorMessage is not null)
        {
            return [ctx.Text($"  Error: {_errorMessage}")];
        }

        var resources = _resources.Values
            .Where(r => !string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name)
            .ToList();

        if (resources.Count == 0)
        {
            return [ctx.Text($"  {MonitorCommandStrings.NoResourcesAvailable}")];
        }

        return [
            ctx.Table<ResourceSnapshot, VStackWidget>(resources)
                .RowKey(r => r.Name)
                .Focus(_focusedResourceKey)
                .OnFocusChanged(key => { _focusedResourceKey = key; })
                .Header(h => [
                    h.Cell("Name").Width(SizeHint.Fill),
                    h.Cell("Type").Fixed(12),
                    h.Cell("State").Fixed(12),
                    h.Cell("Health").Fixed(12),
                    h.Cell("URLs").Width(SizeHint.Fill),
                    h.Cell("Actions").Fixed(20)
                ])
                .Row((r, resource, state) => [
                    r.Cell(resource.DisplayName ?? resource.Name),
                    r.Cell(resource.ResourceType ?? ""),
                    r.Cell(resource.State ?? "Unknown"),
                    r.Cell(resource.HealthStatus ?? ""),
                    r.Cell(resource.Urls.Length > 0
                        ? string.Join(", ", resource.Urls.Select(u => u.Url))
                        : ""),
                    r.Cell(cell => cell.HStack(h => [
                        h.Button("▶").OnClick(e =>
                        {
                            _ = ExecuteResourceCommandAsync(resource.Name, "resource-start");
                        }),
                        h.Button("■").OnClick(e =>
                        {
                            _ = ExecuteResourceCommandAsync(resource.Name, "resource-stop");
                        }),
                        h.Button("↻").OnClick(e =>
                        {
                            _ = ExecuteResourceCommandAsync(resource.Name, "resource-restart");
                        })
                    ]))
                ])
                .Fill()
                .Full()
        ];
    }

    private IEnumerable<Hex1bWidget> BuildParametersTab(WidgetContext<VStackWidget> ctx)
    {
        if (_isConnecting)
        {
            return [ctx.Text($"  {MonitorCommandStrings.ConnectingToAppHost}")];
        }

        var parameters = _resources.Values
            .Where(r => string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name)
            .ToList();

        if (parameters.Count == 0)
        {
            return [ctx.Text($"  {MonitorCommandStrings.NoParametersAvailable}")];
        }

        return [
            ctx.Table<ResourceSnapshot, VStackWidget>(parameters)
                .RowKey(r => r.Name)
                .Focus(_focusedParameterKey)
                .OnFocusChanged(key => { _focusedParameterKey = key; })
                .Header(h => [
                    h.Cell("Name").Width(SizeHint.Fill),
                    h.Cell("State").Fixed(16)
                ])
                .Row((r, param, state) => [
                    r.Cell(param.DisplayName ?? param.Name),
                    r.Cell(param.State ?? "Unknown")
                ])
                .Fill()
                .Full()
        ];
    }

    private async Task ConnectToAppHostAsync(int index, CancellationToken cancellationToken)
    {
        if (_watchCts is not null)
        {
            await _watchCts.CancelAsync().ConfigureAwait(false);
            _watchCts.Dispose();
        }
        _watchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _isConnecting = true;
        _errorMessage = null;
        _resources.Clear();
        _selectedAppHostIndex = index;
        _app?.Invalidate();

        try
        {
            if (index >= _appHosts.Count)
            {
                return;
            }

            var connection = _appHosts[index].Connection;

            var snapshots = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);
            _isConnecting = false;
            foreach (var snapshot in snapshots)
            {
                _resources[snapshot.Name] = snapshot;
            }
            _app?.Invalidate();

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(_watchCts.Token).ConfigureAwait(false))
                    {
                        _resources[snapshot.Name] = snapshot;
                        _app?.Invalidate();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when switching AppHosts or shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error watching resource snapshots");
                    _errorMessage = ex.Message;
                    _app?.Invalidate();
                }
            }, _watchCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error connecting to AppHost");
            _isConnecting = false;
            _errorMessage = ex.Message;
            _app?.Invalidate();
        }
    }

    private async Task ExecuteResourceCommandAsync(string resourceName, string commandName)
    {
        try
        {
            if (_selectedAppHostIndex < _appHosts.Count)
            {
                var connection = _appHosts[_selectedAppHostIndex].Connection;
                await connection.ExecuteResourceCommandAsync(resourceName, commandName, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error executing {Command} on {Resource}", commandName, resourceName);
        }
    }

    private string GetSelectedAppHostTitle()
    {
        if (_appHosts.Count == 0)
        {
            return "No AppHost";
        }

        return _selectedAppHostIndex < _appHosts.Count
            ? _appHosts[_selectedAppHostIndex].DisplayName
            : "Unknown";
    }

    private string GetStatusText()
    {
        if (_isConnecting)
        {
            return MonitorCommandStrings.ConnectingToAppHost;
        }

        var resourceCount = _resources.Values
            .Count(r => !string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase));

        return $"{resourceCount} resource(s)";
    }

    private static string ShortenPath(string path)
    {
        var fileName = Path.GetFileName(path);

        if (string.IsNullOrEmpty(fileName))
        {
            return path;
        }

        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        var directory = Path.GetDirectoryName(path);
        var parentFolder = !string.IsNullOrEmpty(directory)
            ? Path.GetFileName(directory)
            : null;

        return !string.IsNullOrEmpty(parentFolder)
            ? $"{parentFolder}/{fileName}"
            : fileName;
    }
}
