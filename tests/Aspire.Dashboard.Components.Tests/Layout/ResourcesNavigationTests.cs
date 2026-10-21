// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Utils;
using Xunit;
using NavSection = Aspire.Dashboard.Components.Layout.DesktopNavMenu.NavSection;
using ResourceTab = Aspire.Dashboard.Components.Layout.ResourcesLayout.ResourceTab;
using ResourcesPage = Aspire.Dashboard.Components.Pages.Resources;

namespace Aspire.Dashboard.Components.Tests.Layout;

public class ResourcesNavigationTests
{
    [Theory]
    [InlineData("", nameof(NavSection.Home))]
    [InlineData("?resource=frontend", nameof(NavSection.Home))]
    [InlineData("resources", nameof(NavSection.Resources))]
    [InlineData("resources/frontend", nameof(NavSection.Resources))]
    [InlineData("consolelogs/resource/frontend", nameof(NavSection.ConsoleLogs))]
    [InlineData("consolelogs", nameof(NavSection.ConsoleLogs))]
    [InlineData("consolelogs?pane=resources#logs", nameof(NavSection.ConsoleLogs))]
    [InlineData("consolelogs?resource=api&resource=worker", nameof(NavSection.ConsoleLogs))]
    [InlineData("consolelogs?pane=telemetry", nameof(NavSection.ConsoleLogs))]
    [InlineData("structuredlogs?logLevel=error", nameof(NavSection.StructuredLogs))]
    [InlineData("structuredlogs/resource/api", nameof(NavSection.StructuredLogs))]
    [InlineData("structuredlogs?resource=api&resource=worker", nameof(NavSection.StructuredLogs))]
    [InlineData("traces", nameof(NavSection.Traces))]
    [InlineData("traces?resource=api", nameof(NavSection.Traces))]
    [InlineData("traces/detail/abc", nameof(NavSection.Traces))]
    [InlineData("metrics?meter=m#chart", nameof(NavSection.Metrics))]
    [InlineData("metrics/resource/frontend", nameof(NavSection.Metrics))]
    [InlineData("parameters", nameof(NavSection.Parameters))]
    [InlineData("graph#node", nameof(NavSection.Resources))]
    [InlineData("resources?view=graph", nameof(NavSection.Resources))]
    [InlineData("terminals", nameof(NavSection.Terminals))]
    [InlineData("extensions", nameof(NavSection.None))]
    [InlineData("login", nameof(NavSection.None))]
    public void GetSection_WithResourceService(string path, string expectedSection)
    {
        Assert.Equal(Enum.Parse<NavSection>(expectedSection), DesktopNavMenu.GetSection(path, hasResourceService: true));
    }

    [Theory]
    [InlineData("", nameof(NavSection.None))]
    [InlineData("resources/frontend", nameof(NavSection.None))]
    [InlineData("terminals", nameof(NavSection.None))]
    [InlineData("structuredlogs", nameof(NavSection.StructuredLogs))]
    [InlineData("traces/detail/abc", nameof(NavSection.Traces))]
    [InlineData("metrics?meter=m", nameof(NavSection.Metrics))]
    public void GetSection_WithoutResourceService(string path, string expectedSection)
    {
        Assert.Equal(Enum.Parse<NavSection>(expectedSection), DesktopNavMenu.GetSection(path, hasResourceService: false));
    }

    [Theory]
    [InlineData("resources?pane=tags", true)]
    [InlineData("resources?pane=TAGS", true)]
    [InlineData("structuredlogs?pane=telemetry", false)]
    [InlineData("resources?resource=api&pane=resources", false)]
    [InlineData("consolelogs/resource/api?pane=tags#end", true)]
    [InlineData("resources", false)]
    [InlineData("resources?resource=api", false)]
    [InlineData("resources?pane=unknown", false)]
    public void IsTagGroupingRequested_RecognizesLegacyTagsQuery(string path, bool expected)
    {
        Assert.Equal(expected, ResourcesLayout.IsTagGroupingRequested(path));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegacyTelemetryPaneQuery_SelectsCurrentPage(bool hasResourceService)
    {
        Assert.Equal(NavSection.StructuredLogs, DesktopNavMenu.GetSection("structuredlogs?pane=telemetry", hasResourceService));
        Assert.Equal(NavSection.Traces, DesktopNavMenu.GetSection("traces/detail/abc?pane=telemetry", hasResourceService));
    }

    [Theory]
    [InlineData("resources", nameof(ResourceTab.Overview), null, false)]
    [InlineData("resources?resource=", nameof(ResourceTab.Overview), null, false)]
    [InlineData("resources?view=graph", nameof(ResourceTab.Graph), null, false)]
    [InlineData("resources?view=GRAPH#nodes", nameof(ResourceTab.Graph), null, false)]
    [InlineData("resources/frontend", nameof(ResourceTab.Overview), "frontend", false)]
    [InlineData("resources/my%20app?x=1", nameof(ResourceTab.Overview), "my app", false)]
    [InlineData("consolelogs", nameof(ResourceTab.Console), null, false)]
    [InlineData("consolelogs/resource/frontend", nameof(ResourceTab.Console), "frontend", false)]
    [InlineData("structuredlogs/resource/api?logLevel=error", nameof(ResourceTab.StructuredLogs), "api", false)]
    [InlineData("traces", nameof(ResourceTab.Traces), null, false)]
    [InlineData("traces/resource/api", nameof(ResourceTab.Traces), "api", false)]
    [InlineData("traces/detail/abc123", nameof(ResourceTab.Traces), null, true)]
    [InlineData("metrics/resource/api", nameof(ResourceTab.Metrics), "api", false)]
    public void ParseLocation_ReturnsTabAndResource(string path, string expectedTab, string? expectedResourceName, bool expectedKeepSelection)
    {
        var location = ResourcesLayout.ParseLocation(path);

        Assert.Equal(Enum.Parse<ResourceTab>(expectedTab), location.Tab);
        Assert.Equal(expectedResourceName, location.RouteResourceName);
        Assert.Equal(expectedResourceName is null ? [] : [expectedResourceName], location.SelectedResourceNames);
        Assert.Equal(expectedKeepSelection, location.KeepSelection);
    }

    [Theory]
    [InlineData("resources?resource=api&resource=worker", nameof(ResourceTab.Overview), null, new[] { "api", "worker" })]
    [InlineData("consolelogs?resource=api&resource=worker&resource=api", nameof(ResourceTab.Console), null, new[] { "api", "worker" })]
    [InlineData("structuredlogs?logLevel=error&resource=api&resource=my%20app", nameof(ResourceTab.StructuredLogs), null, new[] { "api", "my app" })]
    [InlineData("metrics/resource/worker?resource=api&resource=worker#chart", nameof(ResourceTab.Metrics), "worker", new[] { "api", "worker" })]
    [InlineData("resources/api?resource=", nameof(ResourceTab.Overview), "api", new[] { "api" })]
    public void ParseLocation_ReturnsSelectedResourcesFromQuery(string path, string expectedTab, string? expectedRouteResourceName, string[] expectedSelection)
    {
        var location = ResourcesLayout.ParseLocation(path);

        Assert.Equal(Enum.Parse<ResourceTab>(expectedTab), location.Tab);
        Assert.Equal(expectedRouteResourceName, location.RouteResourceName);
        Assert.Equal(expectedSelection, location.SelectedResourceNames);
        Assert.False(location.KeepSelection);
    }

    [Theory]
    [InlineData(nameof(ResourceTab.Overview), "api", "/resources/api")]
    [InlineData(nameof(ResourceTab.Overview), null, "/resources")]
    [InlineData(nameof(ResourceTab.Graph), null, "/resources?view=graph")]
    [InlineData(nameof(ResourceTab.Console), "api", "/consolelogs/resource/api")]
    [InlineData(nameof(ResourceTab.Console), null, "/consolelogs")]
    [InlineData(nameof(ResourceTab.StructuredLogs), "api", "/structuredlogs/resource/api")]
    [InlineData(nameof(ResourceTab.Traces), null, "/traces")]
    [InlineData(nameof(ResourceTab.Metrics), "api", "/metrics/resource/api")]
    [InlineData(nameof(ResourceTab.Metrics), null, "/metrics")]
    public void GetTabUrl_ReturnsTabUrl(string tab, string? resourceName, string expectedUrl)
    {
        Assert.Equal(expectedUrl, ResourcesLayout.GetTabUrl(Enum.Parse<ResourceTab>(tab), resourceName is null ? [] : [resourceName]));
    }

    [Theory]
    [InlineData(nameof(ResourceTab.Overview), null, "/resources?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Graph), null, "/resources?view=graph&resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Console), null, "/consolelogs?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.StructuredLogs), null, "/structuredlogs?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Traces), "api", "/traces?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Metrics), "worker", "/metrics/resource/worker?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Metrics), "other", "/metrics?resource=api&resource=worker")]
    [InlineData(nameof(ResourceTab.Metrics), null, "/metrics?resource=api&resource=worker")]
    public void GetTabUrl_WithSeveralResources_AddsSelectionToQuery(string tab, string? activeResourceName, string expectedUrl)
    {
        Assert.Equal(expectedUrl, ResourcesLayout.GetTabUrl(Enum.Parse<ResourceTab>(tab), ["api", "worker"], activeResourceName));
    }

    [Theory]
    [InlineData(null, "Graph", "/graph")]
    [InlineData("api", "graph", "/graph")]
    [InlineData(null, "Parameters", "/parameters")]
    [InlineData("api", null, "/resources/api")]
    [InlineData("api", "Table", "/resources/api")]
    [InlineData(null, null, null)]
    [InlineData("", "Table", null)]
    public void TryGetLegacyRedirectUrl_MapsLegacyResourcesQuery(string? resourceName, string? viewKind, string? expectedUrl)
    {
        Assert.Equal(expectedUrl, Home.TryGetLegacyRedirectUrl(resourceName, viewKind));
    }

    [Theory]
    [InlineData("graph", ResourcesPage.ResourceViewKind.Graph)]
    [InlineData("graph?hiddenTypes=Container", ResourcesPage.ResourceViewKind.Graph)]
    [InlineData("parameters", ResourcesPage.ResourceViewKind.Parameters)]
    [InlineData("parameters#top", ResourcesPage.ResourceViewKind.Parameters)]
    [InlineData("", ResourcesPage.ResourceViewKind.Table)]
    public void GetViewKindFromPath_ReturnsRouteViewKind(string path, ResourcesPage.ResourceViewKind expectedViewKind)
    {
        Assert.Equal(expectedViewKind, ResourcesPage.GetViewKindFromPath(path));
    }

    [Fact]
    public void HeaderSectionUrls_MatchSections()
    {
        Assert.Equal(NavSection.Home, DesktopNavMenu.GetSection(DashboardUrls.HomeUrl().TrimStart('/'), hasResourceService: true));
        Assert.Equal(NavSection.Resources, DesktopNavMenu.GetSection(DashboardUrls.ResourceOverviewUrl().TrimStart('/'), hasResourceService: true));
        Assert.Equal(NavSection.Parameters, DesktopNavMenu.GetSection(DashboardUrls.ParametersUrl().TrimStart('/'), hasResourceService: true));
        Assert.Equal(NavSection.Resources, DesktopNavMenu.GetSection(DashboardUrls.GraphUrl().TrimStart('/'), hasResourceService: true));
    }
}
