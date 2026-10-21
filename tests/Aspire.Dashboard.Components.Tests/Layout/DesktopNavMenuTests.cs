// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Dashboard.Utils;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Layout;

public class DesktopNavMenuTests : DashboardTestContext
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void ResourceNavigation_IsConditional(bool enabled, bool hasTerminals, bool hasParameters)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentKeyCode(this);
        FluentUISetupHelpers.SetupFluentMenu(this);
        FluentUISetupHelpers.SetupFluentAnchor(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: enabled));
        Services.GetRequiredService<NavigationManager>().NavigateTo(enabled ? "/terminals/resource/shell" : "/traces/detail/abc");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>(builder => builder
            .Add(p => p.HasResourceTerminals, hasTerminals)
            .Add(p => p.HasParameters, hasParameters));
        var expected = new List<string>();
        if (enabled)
        {
            expected.AddRange([DashboardUrls.HomeUrl(), DashboardUrls.ResourceListUrl()]);
            if (hasParameters)
            {
                expected.Add(DashboardUrls.ParametersUrl());
            }
            expected.Add(DashboardUrls.ConsoleLogsUrl());
        }
        expected.AddRange([DashboardUrls.StructuredLogsUrl(), DashboardUrls.TracesUrl(), DashboardUrls.MetricsUrl()]);
        if (enabled && hasTerminals)
        {
            expected.Add(DashboardUrls.TerminalsUrl());
        }
        var links = cut.FindAll("nav.main-rail > a");
        Assert.Equal(expected, links.Select(link => link.GetAttribute("href")));
        if (enabled && hasTerminals)
        {
            var terminalLink = Assert.Single(links, link => link.GetAttribute("href") == DashboardUrls.TerminalsUrl());
            Assert.Equal(Resources.Layout.NavMenuTerminalsTab, terminalLink.GetAttribute("aria-label"));
            Assert.Equal("page", terminalLink.GetAttribute("aria-current"));
        }
        else if (!enabled)
        {
            var tracesLink = Assert.Single(links, link => link.GetAttribute("href") == DashboardUrls.TracesUrl());
            Assert.Equal(Resources.Layout.NavMenuTracesTab, tracesLink.GetAttribute("aria-label"));
            Assert.Equal("page", tracesLink.GetAttribute("aria-current"));
        }
    }

    [Theory]
    [InlineData("consolelogs", "Console")]
    [InlineData("structuredlogs", "StructuredLogs")]
    [InlineData("traces", "Traces")]
    [InlineData("metrics", "Metrics")]
    public void TelemetryNavigation_PreservesSelectionAndUpdatesActiveItem(string section, string tab)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: true));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/{section}/resource/api?resource=api&resource=worker");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>();

        Assert.Equal($"main-rail-{section}", cut.Find("a[aria-current='page']").Id);
        var shortcut = cut.Find($"#main-rail-{section}");
        var url = shortcut.GetAttribute("href")!;
        var location = ResourcesLayout.ParseLocation(url.TrimStart('/'));
        Assert.Equal(Enum.Parse<ResourcesLayout.ResourceTab>(tab), location.Tab);
        Assert.Equal(["api", "worker"], location.SelectedResourceNames);
        Assert.False(location.KeepSelection);

        navigationManager.NavigateTo(url);
        cut.WaitForAssertion(() => Assert.Equal($"main-rail-{section}", cut.Find("a[aria-current='page']").Id));

        navigationManager.NavigateTo($"/{section}/resource/api");
        cut.WaitForAssertion(() =>
        {
            Assert.Equal($"main-rail-{section}", cut.Find("a[aria-current='page']").Id);
            Assert.Equal(["api"], ResourcesLayout.ParseLocation(cut.Find("#main-rail-traces").GetAttribute("href")!.TrimStart('/')).SelectedResourceNames);
        });
    }

    [Fact]
    public void TraceDetailsNavigation_PreservesSelectionFromResourcePane()
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: true));
        Services.GetRequiredService<ResourcePaneState>().SetSelectedResources(["api", "worker"]);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/traces/detail/abc");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>();

        Assert.Equal("/structuredlogs?resource=api&resource=worker", cut.Find("#main-rail-structuredlogs").GetAttribute("href"));
        Assert.Equal("main-rail-traces", cut.Find("a[aria-current='page']").Id);
    }

    [Theory]
    [InlineData("/traces?resource=api&resource=browser", new[] { "api", "browser" }, new[] { "api" })]
    [InlineData("/structuredlogs/resource/browser", new[] { "browser" }, new string[] { })]
    [InlineData("/metrics?resource=api&resource=worker&resource=browser", new[] { "api", "worker", "browser" }, new[] { "api", "worker" })]
    [InlineData("/traces/detail/abc", new[] { "api", "browser" }, new[] { "api" })]
    [InlineData("/traces/detail/abc", new[] { "browser" }, new string[] { })]
    public void AppModelNavigation_ExcludesTelemetryOnlyResources(string path, string[] selection, string[] appModelSelection)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: true));
        var paneState = Services.GetRequiredService<ResourcePaneState>();
        paneState.SetSelectedResources(selection);
        paneState.SetTelemetryOnlyResources(["browser"]);
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>();

        Assert.Equal(appModelSelection.Length == 0 ? DashboardUrls.ResourceListUrl() :
            ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Overview, appModelSelection),
            cut.Find("#main-rail-resources").GetAttribute("href"));
        Assert.Equal(ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Console, appModelSelection),
            cut.Find("#main-rail-consolelogs").GetAttribute("href"));
        foreach (var section in new[] { "structuredlogs", "traces", "metrics" })
        {
            Assert.Equal(selection,
                ResourcesLayout.ParseLocation(cut.Find($"#main-rail-{section}").GetAttribute("href")!.TrimStart('/')).SelectedResourceNames);
        }
    }

    [Fact]
    public async Task TelemetryResourceChanges_UpdateNavigationSelection()
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: true));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/traces?resource=api&resource=browser");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>();
        Assert.Equal("/resources?resource=api&resource=browser", cut.Find("#main-rail-resources").GetAttribute("href"));

        await cut.InvokeAsync(() => Services.GetRequiredService<ResourcePaneState>().SetTelemetryOnlyResources(["browser"]));

        cut.WaitForAssertion(() => Assert.Equal("/resources/api", cut.Find("#main-rail-resources").GetAttribute("href")));
    }
}
