// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Hex1b;
using Hex1b.Layout;
using Hex1b.Logging;
using Hex1b.Widgets;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.UI;

/// <summary>
/// Represents a summary of resource counts for an AppHost.
/// </summary>
internal sealed class ResourceSummary
{
    public int TotalCount { get; set; }
    public int RunningCount { get; set; }
}

/// <summary>
/// Represents an AppHost entry in the drawer list.
/// </summary>
internal sealed class AppHostEntry
{
    public required string DisplayName { get; init; }
    public required string FullPath { get; init; }
    public required IAppHostAuxiliaryBackchannel Connection { get; set; }
    public bool IsOffline { get; set; }
    public string? Branch { get; set; }
    public ResourceSummary? Summary { get; set; }
    public string? DashboardUrl { get; set; }
    public string? AspireVersion { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public string? Pid { get; set; }
    public string? RepositoryRoot { get; set; }
}

/// <summary>
/// Main TUI for the aspire monitor command.
/// </summary>
internal sealed class AspireAtopTui
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
    private readonly AspireAtopSplash _splash = new();
    private object? _focusedResourceKey;
    private object? _focusedParameterKey;
    private CancellationTokenSource? _watchCts;
    private NotificationStack? _notificationStack;
    private Hex1bApp? _app;
    private IHex1bLogStore? _appHostLogStore;
    private ILoggerFactory? _appHostLoggerFactory;
    private bool _appHostLogsAvailable;

    // Hack reveal transition after splash
    private bool _revealing;
    private long _revealStart;
    private readonly HackRevealEffect _hackReveal = new();
    private const double RevealDurationSeconds = 4.0;

    public AspireAtopTui(IAuxiliaryBackchannelMonitor backchannelMonitor, ILogger logger)
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

        // Resolve git branches and resource summaries in the background (non-blocking)
        _ = ResolveBranchesAsync(_appHosts, cancellationToken);
        _ = FetchResourceSummariesAsync(_appHosts, cancellationToken);

        await using var terminal = Hex1bTerminal.CreateBuilder()
            .WithDiagnostics("aspire-atop", forceEnable: true)
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
            _revealing = true;
            _revealStart = Stopwatch.GetTimestamp();
            _hackReveal.Reset();
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

        if (_revealing)
        {
            var progress = Math.Clamp(
                Stopwatch.GetElapsedTime(_revealStart).TotalSeconds / RevealDurationSeconds, 0, 1);

            if (progress >= 1.0)
            {
                _revealing = false;
                return ctx.ThemePanel(AspireTheme.Apply, BuildMainScreen(ctx, interactive: true)).Fill();
            }

            // During reveal, build without NotificationPanel (it requires ZStack context)
            var revealContent = ctx.ThemePanel(AspireTheme.Apply, BuildMainScreen(ctx, interactive: false)).Fill();

            return ctx.Surface(s =>
            {
                _hackReveal.Update(s.Width, s.Height);
                return
                [
                    s.WidgetLayer(revealContent),
                    s.Layer(_hackReveal.GetCompute(progress))
                ];
            })
            .RedrawAfter(16)
            .Fill();
        }

        return ctx.ThemePanel(AspireTheme.Apply, BuildMainScreen(ctx, interactive: true)).Fill();
    }

    private Hex1bWidget BuildMainScreen(RootContext ctx, bool interactive)
    {
        // When no AppHosts are connected, show a waiting panel
        if (_appHosts.Count == 0)
        {
            return BuildWaitingForAppHostsPanel(ctx, interactive);
        }

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

            // Logger panel for AppHost logs
            Hex1bWidget logPanel = _appHostLogsAvailable && _appHostLogStore is not null
                ? ctx.LoggerPanel(_appHostLogStore).Fill()
                : ctx.Text("  AppHost log streaming is not available in this version of the Aspire SDK.").Fill();

            rightContent = ctx.VStack(v => [
                tabPanel,
                v.DragBarPanel(logPanel).HandleEdge(DragBarEdge.Top).InitialSize(10).MinSize(3)
            ]).Fill();
        }

        // NotificationPanel requires ZStack context — only use in interactive mode
        var mainContent = interactive
            ? ctx.NotificationPanel(rightContent).Fill()
            : rightContent;

        // AppHost panels for the left pane
        var appHostPanels = ctx.VStack(nav => [
            ..BuildAppHostPanels(ctx, nav)
        ]).Fill();

        // Build the content header with AppHost info
        var selectedName = GetSelectedAppHostTitle();
        var dashboardUrl = selectedAppHost?.DashboardUrl;
        var aspireVersion = selectedAppHost?.AspireVersion;
        var startedAt = selectedAppHost?.StartedAt;
        var pid = selectedAppHost?.Pid;
        var repoRoot = selectedAppHost?.RepositoryRoot;

        var resourceCount = _resources.Values
            .Count(r => !string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase));
        var runningCount = _resources.Values
            .Count(r => !string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.State, "Running", StringComparison.OrdinalIgnoreCase));

        var detailParts = new List<string>();
        if (pid is not null)
        {
            detailParts.Add($"⚙ PID {pid}");
        }
        if (aspireVersion is not null && aspireVersion != "unknown")
        {
            detailParts.Add($"◆ {aspireVersion}");
        }
        if (startedAt is not null)
        {
            var uptime = DateTimeOffset.UtcNow - startedAt.Value;
            detailParts.Add($"⏱ {FormatUptime(uptime)}");
        }
        if (resourceCount > 0)
        {
            detailParts.Add($"▣ {runningCount}/{resourceCount}");
        }
        var detailLine = detailParts.Count > 0 ? string.Join("  ·  ", detailParts) : null;

        var headerRows = new List<Func<WidgetContext<VStackWidget>, Hex1bWidget>>();

        // Row 1: AppHost name + detail stats
        headerRows.Add(h => h.HStack(row => [
            row.Text($"▲ {selectedName}"),
            row.Text("").Fill(),
            row.Text(detailLine ?? "").FixedWidth(detailLine?.Length ?? 0)
        ]).FixedHeight(1));

        // Row 2: Dashboard URL + Stop button
        headerRows.Add(h => h.HStack(row => [
            row.Text("⊞ "),
            dashboardUrl is not null
                ? row.Hyperlink(dashboardUrl, dashboardUrl)
                : (Hex1bWidget)row.Text("Dashboard: connecting..."),
            row.Text("").Fill(),
            row.Button(" ⏹ Stop ").OnClick(e =>
            {
                _ = StopSelectedAppHostAsync();
            })
        ]).FixedHeight(1));

        // Row 3: VSCode link (if repo root discovered)
        if (repoRoot is not null)
        {
            var vscodeUrl = $"vscode://file/{Uri.EscapeDataString(repoRoot)}";
            headerRows.Add(h => h.HStack(row => [
                row.Text("⌨ "),
                row.Hyperlink(vscodeUrl, repoRoot)
            ]).FixedHeight(1));
        }

        var headerContent = ctx.VStack(h =>
            headerRows.Select(buildRow => buildRow(h)).ToArray()
        );

        var contentHeader = ctx.ThemePanel(AspireTheme.ApplyContentHeaderBorder,
            ctx.Border(ctx.ThemePanel(AspireTheme.ApplyContentHeaderInner, headerContent)));

        // Main content area: header + tab content
        var rightSide = ctx.VStack(r => [
            contentHeader,
            mainContent
        ]).Fill();

        // Main layout: splitter with AppHost panels on the left, content on the right
        var body = ctx.Padding(1, 1, 0, 0, ctx.HSplitter(
            appHostPanels,
            rightSide,
            leftWidth: 30
        ).Fill()).Fill();

        return ctx.VStack(outer => [
            body,
            outer.InfoBar(bar => [
                bar.Section("⎋ q: " + MonitorCommandStrings.QuitShortcut),
                bar.Separator(" │ "),
                bar.Section("⇥ Tab: " + MonitorCommandStrings.TabShortcut),
                bar.Separator(" │ "),
                bar.Section(GetStatusText()).FillWidth()
            ])
        ]).Fill();
    }

    private static Hex1bWidget BuildWaitingForAppHostsPanel(RootContext ctx, bool interactive)
    {
        var centerContent = ctx.VStack(v => [
            v.Text("").Fill(),
            v.Center(
                ctx.VStack(inner => [
                    inner.Text("  ⏳  Waiting for AppHosts...").FixedHeight(1),
                    inner.Text("").FixedHeight(1),
                    inner.Text("  No running Aspire AppHosts detected.").FixedHeight(1),
                    inner.Text("").FixedHeight(1),
                    inner.Text("  Start an AppHost with 'aspire run' and it will").FixedHeight(1),
                    inner.Text("  appear here automatically.").FixedHeight(1)
                ])
            ),
            v.Text("").Fill()
        ]).Fill();

        return ctx.VStack(outer => [
            interactive ? ctx.NotificationPanel(centerContent).Fill() : centerContent,
            outer.InfoBar(bar => [
                bar.Section("q: " + MonitorCommandStrings.QuitShortcut),
                bar.Separator(" │ "),
                bar.Section("Polling for AppHosts...").FillWidth()
            ])
        ]).Fill();
    }

    private IEnumerable<Hex1bWidget> BuildAppHostPanels(RootContext ctx, WidgetContext<VStackWidget> nav)
    {
        if (_appHosts.Count == 0)
        {
            return [nav.Text($"  {MonitorCommandStrings.NoRunningAppHostsFound}")];
        }

        return _appHosts.Select((appHost, i) =>
        {
            var branchName = appHost.Branch ?? "unknown";
            var index = i;

            var interactable = nav.Interactable(ic =>
            {
                var focused = ic.IsFocused || ic.IsHovered;
                var innerContent = ctx.ThemePanel(
                    focused ? AspireTheme.ApplyAppHostTileInnerFocused : AspireTheme.ApplyAppHostTileInner,
                    ic.VStack(v => [
                        v.Text($"▲ {appHost.DisplayName}").FixedHeight(1),
                        v.Text($" ⎇ {branchName}").FixedHeight(1)
                    ]));

                return (Hex1bWidget)ctx.ThemePanel(
                    focused
                        ? AspireTheme.ApplyAppHostTileFocused
                        : AspireTheme.ApplyAppHostTile,
                    ctx.Border(innerContent));
            }).OnClick(args =>
            {
                _ = ConnectToAppHostAsync(index, CancellationToken.None);
            });

            return (Hex1bWidget)interactable;
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

        var table = ctx.Table<ResourceSnapshot, VStackWidget>(resources)
            .RowKey(r => r.Name)
            .Focus(_focusedResourceKey)
            .OnFocusChanged(key =>
            {
                _focusedResourceKey = key;
            })
            .Header(h => [
                h.Cell("Name").Width(SizeHint.Fill),
                h.Cell("Type").Fixed(12),
                h.Cell("State").Fixed(12),
                h.Cell("Health").Fixed(14),
                h.Cell("URLs").Width(SizeHint.Fill),
                h.Cell("Actions").Fixed(20)
            ])
            .Row((r, resource, state) => [
                r.Cell(resource.DisplayName ?? resource.Name),
                r.Cell(resource.ResourceType ?? ""),
                r.Cell(resource.State ?? "Unknown"),
                r.Cell(FormatHealthStatus(resource.HealthStatus)),
                r.Cell(cell => BuildUrlsCell(cell, resource)),
                r.Cell(cell => cell.HStack(h => [
                    h.Button("▶").OnClick(e =>
                    {
                        _ = ExecuteResourceCommandAsync(resource.Name, "resource-start");
                    }),
                    h.Button("⏹").OnClick(e =>
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
            .Full();

        return [table];
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
                        var wasEmpty = _appHosts.Count == 0;
                        var entry = new AppHostEntry
                        {
                            DisplayName = ShortenPath(path),
                            FullPath = path,
                            Connection = connection
                        };
                        _appHosts.Add(entry);
                        knownPaths.Add(path);

                        // Resolve branch and resource summary in the background
                        _ = ResolveBranchAsync(entry, cancellationToken);
                        _ = FetchResourceSummaryAsync(entry, cancellationToken);

                        // Auto-connect if this is the first AppHost
                        if (wasEmpty)
                        {
                            _ = ConnectToAppHostAsync(0, cancellationToken);
                        }
                        else
                        {
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
                        }

                        _app?.Invalidate();
                    }
                    else
                    {
                        // Check if a previously offline AppHost came back
                        var existing = _appHosts.FirstOrDefault(a => a.FullPath == path && a.IsOffline);
                        if (existing is not null)
                        {
                            existing.IsOffline = false;
                            // Update to the fresh connection since the old one is stale
                            existing.Connection = connection;

                            _notificationStack?.Post(
                                new Notification("AppHost Back Online", existing.DisplayName)
                                    .Timeout(TimeSpan.FromSeconds(5)));

                            // Auto-reconnect if this is the currently selected AppHost
                            var idx = _appHosts.IndexOf(existing);
                            if (idx == _selectedAppHostIndex)
                            {
                                _ = ConnectToAppHostAsync(idx, cancellationToken);
                            }

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

    private async Task ResolveBranchesAsync(List<AppHostEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            await ResolveBranchAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ResolveBranchAsync(AppHostEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            entry.Branch = await GitBranchHelper.GetCurrentBranchAsync(entry.FullPath, cancellationToken).ConfigureAwait(false);
            _app?.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve git branch for {Path}", entry.FullPath);
        }
    }

    private async Task FetchResourceSummariesAsync(List<AppHostEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            await FetchResourceSummaryAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FetchResourceSummaryAsync(AppHostEntry entry, CancellationToken cancellationToken)
    {
        if (entry.IsOffline)
        {
            return;
        }

        try
        {
            var snapshots = await entry.Connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);
            var resources = snapshots.Where(r => !string.Equals(r.ResourceType, "Parameter", StringComparison.OrdinalIgnoreCase)).ToList();
            entry.Summary = new ResourceSummary
            {
                TotalCount = resources.Count,
                RunningCount = resources.Count(r => string.Equals(r.State, "Running", StringComparison.OrdinalIgnoreCase))
            };
            _app?.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch resource summary for {Path}", entry.FullPath);
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

        // Tear down previous state
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

            // Fetch dashboard URL
            try
            {
                var dashboardInfo = await connection.GetDashboardInfoV2Async(cancellationToken).ConfigureAwait(false);
                if (dashboardInfo?.DashboardUrls is { Length: > 0 } urls)
                {
                    _appHosts[index].DashboardUrl = urls[0];
                }
            }
            catch
            {
                // Dashboard info is best-effort
            }

            // Fetch AppHost info (version, PID, start time)
            try
            {
                var appHostInfo = connection.AppHostInfo;
                if (appHostInfo is not null)
                {
                    _appHosts[index].StartedAt = appHostInfo.StartedAt;
                    _appHosts[index].Pid = appHostInfo.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (connection is AppHostAuxiliaryBackchannel { SupportsV2: true } v2Connection)
                {
                    var v2Info = await v2Connection.GetAppHostInfoV2Async(cancellationToken).ConfigureAwait(false);
                    if (v2Info is not null)
                    {
                        _appHosts[index].AspireVersion = v2Info.AspireHostVersion;
                        _appHosts[index].StartedAt = v2Info.StartedAt;
                        _appHosts[index].Pid = v2Info.Pid;

                        // Discover repo root on the CLI side from the AppHost path
                        if (v2Info.AppHostPath is not null)
                        {
                            _appHosts[index].RepositoryRoot = await GitBranchHelper.GetRepositoryRootAsync(v2Info.AppHostPath, cancellationToken).ConfigureAwait(false);
                        }

                        _logger.LogDebug("V2 info: Pid={Pid}, RepoRoot={RepoRoot}, AppHostPath={Path}",
                            v2Info.Pid, _appHosts[index].RepositoryRoot ?? "(null)", v2Info.AppHostPath);
                    }
                }
            }
            catch
            {
                // AppHost info is best-effort
            }

            // Set up AppHost log streaming
            _appHostLoggerFactory?.Dispose();
            _appHostLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddHex1b(out var logStore);
                _appHostLogStore = logStore;
            });
            _appHostLogsAvailable = false;

            try
            {
                var logStream = await connection.GetAppHostLogEntriesAsync(cancellationToken).ConfigureAwait(false);
                if (logStream is not null)
                {
                    _appHostLogsAvailable = true;
                    var appHostLogger = _appHostLoggerFactory.CreateLogger("AppHost");

                    // Log a startup message to confirm the panel is connected
                    appHostLogger.LogInformation("Connected to AppHost log stream");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await foreach (var entry in logStream.WithCancellation(_watchCts.Token).ConfigureAwait(false))
                            {
                                appHostLogger.Log(entry.LogLevel, "{Message}", entry.Message);
                                _app?.Invalidate();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when switching AppHosts
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error streaming AppHost logs");
                        }
                    }, _watchCts.Token);
                }
                else
                {
                    _logger.LogDebug("AppHost log streaming returned null - not supported by this AppHost");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AppHost log streaming is not available");
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

    private async Task StopSelectedAppHostAsync()
    {
        try
        {
            if (_selectedAppHostIndex < _appHosts.Count)
            {
                var appHost = _appHosts[_selectedAppHostIndex];
                var stopped = await appHost.Connection.StopAppHostAsync(CancellationToken.None).ConfigureAwait(false);
                if (stopped)
                {
                    _notificationStack?.Post(
                        new Notification("AppHost Stopped", $"{appHost.DisplayName} has been stopped.")
                            .Timeout(TimeSpan.FromSeconds(5)));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping AppHost");
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

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h";
        }
        if (uptime.TotalHours >= 1)
        {
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        }
        return $"{(int)uptime.TotalMinutes}m";
    }

    private static string FormatHealthStatus(string? healthStatus)
    {
        if (string.IsNullOrEmpty(healthStatus))
        {
            return "";
        }

        var icon = string.Equals(healthStatus, "Healthy", StringComparison.OrdinalIgnoreCase)
            ? "💚"
            : "💔";

        return $"{icon} {healthStatus}";
    }

    private static Hex1bWidget BuildUrlsCell(TableCellContext cell, ResourceSnapshot resource)
    {
        if (resource.Urls.Length == 0)
        {
            return cell.Text("");
        }

        return cell.HStack(h =>
            resource.Urls.Select(u => (Hex1bWidget)h.Hyperlink(u.Url, u.Url)).ToArray());
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
