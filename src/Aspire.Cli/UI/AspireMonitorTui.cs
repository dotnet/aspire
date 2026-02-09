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
    public bool IsOffline { get; set; }
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
    private readonly AspireMonitorSplash _splash = new();
    private object? _focusedResourceKey;
    private object? _focusedParameterKey;
    private CancellationTokenSource? _watchCts;
    private NotificationStack? _notificationStack;
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

        // Animate the splash, then transition to main screen
        _ = Task.Run(async () =>
        {
            // Drive the animation by invalidating at ~30fps until all phases complete
            while (!_splash.IsComplete)
            {
                _app?.Invalidate();
                await Task.Delay(33, cancellationToken).ConfigureAwait(false);
            }

            _showSplash = false;
            _app?.Invalidate();

            if (_appHosts.Count > 0)
            {
                await ConnectToAppHostAsync(0, cancellationToken).ConfigureAwait(false);
            }

            // Start background polling for new/offline AppHosts
            _ = PollAppHostsAsync(cancellationToken);
        }, cancellationToken);

        await terminal.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private Hex1bWidget BuildWidget(RootContext ctx)
    {
        if (_showSplash)
        {
            return _splash.Build(ctx);
        }

        return ctx.ThemePanel(AspireTheme.Apply, BuildMainScreen(ctx)).Fill();
    }

    private Hex1bWidget BuildMainScreen(RootContext ctx)
    {
        // Build right-side content depending on selected AppHost state
        Hex1bWidget rightContent;
        var selectedAppHost = _selectedAppHostIndex < _appHosts.Count ? _appHosts[_selectedAppHostIndex] : null;

        if (selectedAppHost?.IsOffline == true)
        {
            rightContent = BuildOfflinePanel(ctx, selectedAppHost);
        }
        else
        {
            // Tab panel with Resources and Parameters tabs
            var tabPanel = ctx.TabPanel(tabs => [
                tabs.Tab(MonitorCommandStrings.ResourcesTab, c => BuildResourcesTab(c)),
                tabs.Tab(MonitorCommandStrings.ParametersTab, c => BuildParametersTab(c))
            ]).Fill();

            rightContent = tabPanel;
        }

        // Wrap content in notification panel
        var mainContent = ctx.NotificationPanel(rightContent).Fill();

        // AppHost list for the left pane
        var appHostList = ctx.VStack(nav => [
            nav.Text($" {MonitorCommandStrings.AppHostsDrawerTitle}").FixedHeight(1),
            nav.Separator(),
            ..BuildAppHostList(nav)
        ]).Fill();

        // Main layout: splitter with AppHost list on the left, content on the right
        var body = ctx.HSplitter(
            ctx.Border(appHostList, title: "App Hosts"),
            ctx.Border(mainContent, title: GetSelectedAppHostTitle()).Fill(),
            leftWidth: 30
        ).Fill();

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
        {
            var prefix = _selectedAppHostIndex == i ? " ▸ " : "   ";
            var suffix = appHost.IsOffline ? " ⚠" : "";
            return nav.Button($"{prefix}{appHost.DisplayName}{suffix}")
                .OnClick(e =>
                {
                    _notificationStack ??= e.Context.Notifications;

                    if (i != _selectedAppHostIndex)
                    {
                        _selectedAppHostIndex = i;
                        if (!appHost.IsOffline)
                        {
                            _ = ConnectToAppHostAsync(i, CancellationToken.None);
                        }
                    }
                });
        });
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

    private Hex1bWidget BuildOfflinePanel(RootContext ctx, AppHostEntry appHost)
    {
        return ctx.VStack(v => [
            v.Text("").Fill(),
            v.Center(
                ctx.VStack(inner => [
                    inner.Text("⚠ AppHost Offline").FixedHeight(1),
                    inner.Text("").FixedHeight(1),
                    inner.Text($"{appHost.DisplayName} is no longer running.").FixedHeight(1),
                    inner.Text("").FixedHeight(1),
                    inner.HStack(buttons => [
                        buttons.Button(" Remove from list ").OnClick(e =>
                        {
                            _notificationStack ??= e.Context.Notifications;
                            var idx = _appHosts.IndexOf(appHost);
                            _appHosts.Remove(appHost);
                            if (_appHosts.Count == 0)
                            {
                                _selectedAppHostIndex = 0;
                            }
                            else if (_selectedAppHostIndex >= _appHosts.Count)
                            {
                                _selectedAppHostIndex = _appHosts.Count - 1;
                                _ = ConnectToAppHostAsync(_selectedAppHostIndex, CancellationToken.None);
                            }
                            else if (idx == _selectedAppHostIndex)
                            {
                                _ = ConnectToAppHostAsync(_selectedAppHostIndex, CancellationToken.None);
                            }
                        }),
                        buttons.Text("  ").FixedWidth(2),
                        buttons.Button(" Try reconnecting ").OnClick(e =>
                        {
                            _notificationStack ??= e.Context.Notifications;
                            appHost.IsOffline = false;
                            _ = ConnectToAppHostAsync(_appHosts.IndexOf(appHost), CancellationToken.None);
                        })
                    ]).FixedHeight(1)
                ])
            ),
            v.Text("").Fill()
        ]).Fill();
    }

    private async Task PollAppHostsAsync(CancellationToken cancellationToken)
    {
        var knownPaths = new HashSet<string>(_appHosts.Select(a => a.FullPath));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
                await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);

                var currentConnections = _backchannelMonitor.Connections
                    .Where(c => c.AppHostInfo is not null)
                    .ToList();

                var currentPaths = new HashSet<string>(
                    currentConnections.Select(c => c.AppHostInfo!.AppHostPath ?? "Unknown"));

                // Detect new AppHosts
                foreach (var connection in currentConnections)
                {
                    var path = connection.AppHostInfo!.AppHostPath ?? "Unknown";
                    if (!knownPaths.Contains(path))
                    {
                        var entry = new AppHostEntry
                        {
                            DisplayName = ShortenPath(path),
                            FullPath = path,
                            Connection = connection
                        };
                        _appHosts.Add(entry);
                        knownPaths.Add(path);

                        _notificationStack?.Post(
                            new Notification("New AppHost Detected", entry.DisplayName)
                                .Timeout(TimeSpan.FromSeconds(10))
                                .PrimaryAction("Connect", async ctx =>
                                {
                                    var idx = _appHosts.IndexOf(entry);
                                    if (idx >= 0)
                                    {
                                        await ConnectToAppHostAsync(idx, cancellationToken).ConfigureAwait(false);
                                    }
                                    ctx.Dismiss();
                                }));

                        _app?.Invalidate();
                    }
                    else
                    {
                        // Check if a previously offline AppHost came back
                        var existing = _appHosts.FirstOrDefault(a => a.FullPath == path && a.IsOffline);
                        if (existing is not null)
                        {
                            existing.IsOffline = false;

                            _notificationStack?.Post(
                                new Notification("AppHost Back Online", existing.DisplayName)
                                    .Timeout(TimeSpan.FromSeconds(5)));

                            _app?.Invalidate();
                        }
                    }
                }

                // Detect offline AppHosts
                foreach (var appHost in _appHosts)
                {
                    if (!appHost.IsOffline && !currentPaths.Contains(appHost.FullPath))
                    {
                        appHost.IsOffline = true;
                        _app?.Invalidate();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error polling for AppHosts");
            }
        }
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

                    // Stream ended normally — AppHost likely shut down
                    if (index < _appHosts.Count)
                    {
                        _appHosts[index].IsOffline = true;
                        _resources.Clear();
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

                    // Connection lost — mark as offline
                    if (index < _appHosts.Count)
                    {
                        _appHosts[index].IsOffline = true;
                        _resources.Clear();
                    }

                    _app?.Invalidate();
                }
            }, _watchCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error connecting to AppHost");
            _isConnecting = false;

            if (index < _appHosts.Count)
            {
                _appHosts[index].IsOffline = true;
            }

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
