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
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TerminalsNavigation_IsConditional(bool enabled, bool hasTerminals)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentKeyCode(this);
        FluentUISetupHelpers.SetupFluentMenu(this);
        FluentUISetupHelpers.SetupFluentAnchor(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: enabled));
        if (!enabled)
        {
            Services.GetRequiredService<ResourcePaneState>().SetMode(ResourcePaneMode.Tags);
        }
        Services.GetRequiredService<NavigationManager>().NavigateTo(enabled ? "/terminals/resource/shell" : "/traces/detail/abc");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>(builder => builder.Add(p => p.HasResourceTerminals, hasTerminals));
        var expected = new List<string>();
        if (enabled)
        {
            expected.AddRange([DashboardUrls.HomeUrl(), DashboardUrls.ResourceListUrl(),
                DashboardUrls.ParametersUrl(), DashboardUrls.GraphUrl()]);
            if (hasTerminals)
            {
                expected.Add(DashboardUrls.TerminalsUrl());
            }
            expected.Add(DashboardUrls.ExtensionsUrl());
        }
        expected.Add(DashboardUrls.TelemetrySourcesUrl());
        if (enabled)
        {
            expected.Add(DashboardUrls.ConsoleLogsUrl());
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
            var structuredLogsLink = Assert.Single(links, link => link.GetAttribute("href") == DashboardUrls.TelemetrySourcesUrl());
            Assert.Equal(Resources.Layout.NavMenuTelemetrySourcesTab, structuredLogsLink.GetAttribute("aria-label"));
            Assert.Equal("page", structuredLogsLink.GetAttribute("aria-current"));
        }
    }

    [Fact]
    public void ConsoleLogsNavigation_SelectsAllResourcesAndUpdatesActiveItem()
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: true));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/consolelogs/resource/api?resource=api&resource=worker");
        Render<FluentTooltipProvider>();
        var cut = Render<DesktopNavMenu>();

        Assert.Equal("main-rail-resources", cut.Find("a[aria-current='page']").Id);
        var shortcut = cut.Find("#main-rail-consolelogs");
        Assert.Equal(Resources.Layout.NavMenuConsoleLogsTab, shortcut.GetAttribute("aria-label"));
        Assert.Equal("HR", shortcut.PreviousElementSibling?.TagName);
        var url = shortcut.GetAttribute("href")!;
        var location = ResourcesLayout.ParseLocation(url.TrimStart('/'));
        Assert.Equal(ResourcesLayout.ResourceTab.Console, location.Tab);
        Assert.Empty(location.SelectedResourceNames);
        Assert.False(location.KeepSelection);

        navigationManager.NavigateTo(url);
        cut.WaitForAssertion(() => Assert.Equal("main-rail-consolelogs", cut.Find("a[aria-current='page']").Id));

        navigationManager.NavigateTo("/consolelogs/resource/api");
        cut.WaitForAssertion(() => Assert.Equal("main-rail-resources", cut.Find("a[aria-current='page']").Id));
    }
}
