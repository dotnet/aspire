// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Layout;

/// <summary>
/// The top-level navigation displayed as a vertical icon rail on the left of the desktop layout. With a resource
/// service the dashboard is organized around the app model, with global telemetry links below Parameters
/// preserving the resource selection. Telemetry-only sources are excluded when navigating to app model pages.
/// </summary>
public partial class DesktopNavMenu : ComponentBase, IDisposable
{
    internal static Icon HomeIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.Home()
                  : new Icons.Regular.Size24.Home();

    internal static Icon ResourcesIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.AppFolder()
                  : new Icons.Regular.Size24.AppFolder();

    // WindowConsole only ships at Size20 in this Fluent version, so callers scale it to 24px. The Size24 alternatives
    // (WindowDevTools, Code) read as "developer tools" rather than "terminal".
    internal static Icon TerminalsIcon(bool active = false) =>
        active ? new Icons.Filled.Size20.WindowConsole()
                  : new Icons.Regular.Size20.WindowConsole();

    internal static Icon ParametersIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.Key()
                  : new Icons.Regular.Size24.Key();

    internal static Icon GraphIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.ShareAndroid()
                  : new Icons.Regular.Size24.ShareAndroid();

    internal static Icon ConsoleLogsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.SlideText()
                  : new Icons.Regular.Size24.SlideText();

    [Parameter]
    public bool HasResourceTerminals { get; set; }

    [Parameter]
    public bool HasParameters { get; set; }

    internal static Icon StructuredLogsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.SlideTextSparkle()
                  : new Icons.Regular.Size24.SlideTextSparkle();

    internal static Icon TracesIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.GanttChart()
                  : new Icons.Regular.Size24.GanttChart();

    internal static Icon MetricsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.ChartMultiple()
                  : new Icons.Regular.Size24.ChartMultiple();

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ResourcePaneState ResourcePaneState { get; init; }

    private NavSection _activeSection;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        ResourcePaneState.Changed += OnResourcePaneChanged;
        _activeSection = GetSection(NavigationManager.ToBaseRelativePath(NavigationManager.Uri), DashboardClient.IsEnabled);
    }

    private void OnResourcePaneChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _activeSection = GetSection(NavigationManager.ToBaseRelativePath(e.Location), DashboardClient.IsEnabled);
        _ = InvokeAsync(StateHasChanged);
    }

    private IEnumerable<NavItem> GetItems() => GetItems(DashboardClient.IsEnabled, HasResourceTerminals, HasParameters, Loc,
        NavigationManager.ToBaseRelativePath(NavigationManager.Uri), ResourcePaneState);

    internal static IEnumerable<NavItem> GetItems(bool hasResourceService, bool hasResourceTerminals, bool hasParameters,
        IStringLocalizer<Resources.Layout> loc, string baseRelativePath, ResourcePaneState paneState)
    {
        var location = ResourcesLayout.ParseLocation(baseRelativePath);
        var section = GetSection(baseRelativePath, hasResourceService);
        IReadOnlyList<string> resourceNames = section is NavSection.Resources or NavSection.ConsoleLogs or NavSection.StructuredLogs or NavSection.Traces or NavSection.Metrics
            ? location.KeepSelection ? paneState.SelectedResourceNames : location.SelectedResourceNames
            : [];

        if (hasResourceService)
        {
            var appModelResourceNames = paneState.ExcludeTelemetryOnlyResources(resourceNames);
            yield return new NavItem(NavSection.Home, DashboardUrls.HomeUrl(), loc[nameof(Resources.Layout.NavMenuHomeTab)], HomeIcon(), HomeIcon(active: true));
            yield return new NavItem(NavSection.Resources,
                appModelResourceNames.Count == 0 ? DashboardUrls.ResourceListUrl() : ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Overview, appModelResourceNames),
                loc[nameof(Resources.Layout.NavMenuResourcesTab)], ResourcesIcon(), ResourcesIcon(active: true));
            if (hasParameters)
            {
                yield return new NavItem(NavSection.Parameters, DashboardUrls.ParametersUrl(), loc[nameof(Resources.Layout.NavMenuParametersTab)], ParametersIcon(), ParametersIcon(active: true));
            }
            yield return new NavItem(NavSection.ConsoleLogs,
                ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Console, appModelResourceNames),
                loc[nameof(Resources.Layout.NavMenuConsoleLogsTab)], ConsoleLogsIcon(), ConsoleLogsIcon(active: true));
        }
        yield return new NavItem(NavSection.StructuredLogs, ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.StructuredLogs, resourceNames), loc[nameof(Resources.Layout.NavMenuStructuredLogsTab)], StructuredLogsIcon(), StructuredLogsIcon(active: true));
        yield return new NavItem(NavSection.Traces, ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Traces, resourceNames), loc[nameof(Resources.Layout.NavMenuTracesTab)], TracesIcon(), TracesIcon(active: true));
        yield return new NavItem(NavSection.Metrics, ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Metrics, resourceNames), loc[nameof(Resources.Layout.NavMenuMetricsTab)], MetricsIcon(), MetricsIcon(active: true));
        if (hasResourceService && hasResourceTerminals)
        {
            yield return new NavItem(NavSection.Terminals, DashboardUrls.TerminalsUrl(), loc[nameof(Resources.Layout.NavMenuTerminalsTab)], TerminalsIcon(), TerminalsIcon(active: true));
        }
    }

    /// <summary>
    /// Maps a base-relative path (e.g. <c>traces/resource/api?type=http</c>) to the top-level section it belongs to.
    /// </summary>
    internal static NavSection GetSection(string baseRelativePath, bool hasResourceService)
    {
        var path = baseRelativePath;
        var queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        var firstSegment = path.Split('/', 2)[0];

        if (!hasResourceService)
        {
            return firstSegment switch
            {
                DashboardUrls.StructuredLogsBasePath => NavSection.StructuredLogs,
                DashboardUrls.TracesBasePath => NavSection.Traces,
                DashboardUrls.MetricsBasePath => NavSection.Metrics,
                _ => NavSection.None
            };
        }

        return firstSegment switch
        {
            "" => NavSection.Home,
            DashboardUrls.ParametersBasePath => NavSection.Parameters,
            DashboardUrls.GraphBasePath => NavSection.Resources,
            DashboardUrls.TerminalsBasePath => NavSection.Terminals,
            DashboardUrls.ResourceOverviewBasePath => NavSection.Resources,
            DashboardUrls.ConsoleLogBasePath => NavSection.ConsoleLogs,
            DashboardUrls.StructuredLogsBasePath => NavSection.StructuredLogs,
            DashboardUrls.TracesBasePath => NavSection.Traces,
            DashboardUrls.MetricsBasePath => NavSection.Metrics,
            _ => NavSection.None
        };
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        ResourcePaneState.Changed -= OnResourcePaneChanged;
    }

    internal enum NavSection
    {
        None,
        Home,
        Resources,
        Parameters,
        Terminals,
        ConsoleLogs,
        StructuredLogs,
        Traces,
        Metrics
    }

    internal sealed record NavItem(NavSection Section, string Href, string Text, Icon Icon, Icon ActiveIcon);
}
