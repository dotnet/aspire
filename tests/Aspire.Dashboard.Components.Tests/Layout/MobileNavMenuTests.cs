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
using Microsoft.JSInterop;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Layout;

[UseCulture("en-US")]
public class MobileNavMenuTests : DashboardTestContext
{
    [Theory]
    [InlineData("/structuredlogs?logLevel=error", nameof(Resources.Layout.NavMenuStructuredLogsTab))]
    [InlineData("/traces/detail/abc", nameof(Resources.Layout.NavMenuTracesTab))]
    [InlineData("/metrics/resource/api", nameof(Resources.Layout.NavMenuMetricsTab))]
    public void Standalone_TelemetryPagesSelectPageEntry(string path, string expectedText)
    {
        var cut = RenderMobileNavMenu(path, hasResourceService: false, hasParameters: true);

        AssertMenuItemIsActive(cut, Resources.Layout.ResourceManager.GetString(expectedText)!);
        Assert.Equal(
            [Resources.Layout.NavMenuStructuredLogsTab, Resources.Layout.NavMenuTracesTab, Resources.Layout.NavMenuMetricsTab,
                Resources.Layout.MainLayoutAspireRepoLink,
                Resources.Layout.MainLayoutKeyboardShortcuts,
                Resources.Layout.MainLayoutAspireDashboardHelpLink, Resources.Layout.MainLayoutLaunchNotifications,
                Resources.Layout.MainLayoutLaunchSettings],
            cut.FindAll("fluent-menu-item").Select(item => item.GetAttribute("data-tooltip")));
    }

    [Fact]
    public async Task Documentation_OpensDashboardDocsInNewTabAndClosesMenu()
    {
        JSInterop.SetupVoid("open", _ => true).SetVoidResult();
        var menuClosed = false;
        var cut = RenderMobileNavMenu(DashboardUrls.HomeUrl(), () => menuClosed = true);
        var documentationItem = cut.FindAll("fluent-menu-item")
            .Single(item => item.GetAttribute("data-tooltip") == Resources.Layout.MainLayoutAspireDashboardHelpLink);

        await cut.InvokeAsync(() => documentationItem.TriggerEvent("onmenuitemchange",
            new MenuItemEventArgs { Id = documentationItem.Id, Text = documentationItem.TextContent }));

        Assert.True(menuClosed);
        var invocation = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "open");
        Assert.Equal(["https://aspire.dev/dashboard/overview/", "_blank", "noopener,noreferrer"], invocation.Arguments);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ResourceNavigation_IsConditional(bool hasTerminals, bool hasParameters)
    {
        var cut = RenderMobileNavMenu("/terminals/resource/shell", hasResourceTerminals: hasTerminals, hasParameters: hasParameters);
        var titles = cut.FindAll("fluent-menu-item").Select(i => i.GetAttribute("data-tooltip")).Take(7 + (hasTerminals ? 1 : 0) + (hasParameters ? 1 : 0));
        var expected = new List<string>
        {
            Resources.Layout.NavMenuHomeTab,
            Resources.Layout.NavMenuResourcesTab
        };
        if (hasParameters)
        {
            expected.Add(Resources.Layout.NavMenuParametersTab);
        }
        expected.AddRange([
            Resources.Layout.NavMenuConsoleLogsTab,
            Resources.Layout.NavMenuStructuredLogsTab,
            Resources.Layout.NavMenuTracesTab,
            Resources.Layout.NavMenuMetricsTab
        ]);
        if (hasTerminals)
        {
            expected.Add(Resources.Layout.NavMenuTerminalsTab);
        }
        expected.Add(Resources.Layout.MainLayoutAspireRepoLink);
        Assert.Equal(expected, titles);
        if (hasTerminals)
        {
            AssertMenuItemIsActive(cut, Resources.Layout.NavMenuTerminalsTab);
        }
    }

    [Fact]
    public void AppHost_LegacyTelemetryPaneQuerySelectsPageEntry()
    {
        var cut = RenderMobileNavMenu("/structuredlogs?pane=telemetry");
        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuStructuredLogsTab);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task AppModelNavigation_ExcludesTelemetryOnlyResourcesAndClosesMenu(bool consoleLogs, bool mixedSelection, bool traceDetails)
    {
        var menuClosed = false;
        string[] selection = mixedSelection ? ["api", "browser"] : ["browser"];
        var path = traceDetails ? "/traces/detail/abc" : ResourcesLayout.GetTabUrl(ResourcesLayout.ResourceTab.Traces, selection);
        var cut = RenderMobileNavMenu(path, () => menuClosed = true);
        var paneState = Services.GetRequiredService<ResourcePaneState>();
        await cut.InvokeAsync(() =>
        {
            paneState.SetSelectedResources(selection);
            paneState.SetTelemetryOnlyResources(["browser"]);
        });
        var item = cut.FindAll("fluent-menu-item").Single(item => item.GetAttribute("data-tooltip") ==
            (consoleLogs ? Resources.Layout.NavMenuConsoleLogsTab : Resources.Layout.NavMenuResourcesTab));

        await cut.InvokeAsync(() => item.TriggerEvent("onmenuitemchange",
            new MenuItemEventArgs { Id = item.Id, Text = item.TextContent }));

        var expectedUrl = mixedSelection
            ? consoleLogs ? DashboardUrls.ConsoleLogsUrl("api") : DashboardUrls.ResourceOverviewUrl("api")
            : consoleLogs ? DashboardUrls.ConsoleLogsUrl() : DashboardUrls.ResourceListUrl();
        Assert.True(menuClosed);
        Assert.Equal(Services.GetRequiredService<NavigationManager>().ToAbsoluteUri(expectedUrl).AbsoluteUri,
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task TelemetryNavigation_PreservesSelectionAndClosesMenu()
    {
        var menuClosed = false;
        var cut = RenderMobileNavMenu("/resources?resource=api&resource=worker", () => menuClosed = true);
        var tracesItem = cut.FindAll("fluent-menu-item")
            .Single(item => item.GetAttribute("data-tooltip") == Resources.Layout.NavMenuTracesTab);

        await cut.InvokeAsync(() => tracesItem.TriggerEvent("onmenuitemchange",
            new MenuItemEventArgs { Id = tracesItem.Id, Text = tracesItem.TextContent }));

        Assert.True(menuClosed);
        Assert.Equal("http://localhost/traces?resource=api&resource=worker", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Render_OpenMenu_TelemetryPageWithQueryStringSelectsPageEntry()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.StructuredLogsUrl(logLevel: "warning"));

        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuStructuredLogsTab);
    }

    [Fact]
    public void Render_OpenMenu_TelemetryPageSelectsPageEntry()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.StructuredLogsUrl());

        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuStructuredLogsTab);
    }

    [Fact]
    public void Render_OpenMenu_ResourceOverviewPageHasSemanticAndVisualSelectedState()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.ResourceOverviewUrl("foo"));

        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuResourcesTab);
    }

    [Fact]
    public void Render_OpenMenu_HomePageHasSemanticAndVisualSelectedState()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.HomeUrl());

        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuHomeTab);
    }

    [Fact]
    public void Render_OpenMenu_ParametersPageHasSemanticAndVisualSelectedState()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.ParametersUrl(hiddenStates: "Running"), hasParameters: true);

        AssertMenuItemIsActive(cut, Resources.Layout.NavMenuParametersTab);
    }

    [Fact]
    public void MobileNavMenu_ConstrainedToRemainingViewport()
    {
        var cut = RenderMobileNavMenu(DashboardUrls.HomeUrl());

        var menu = cut.Find("fluent-menu-list");
        var style = menu.GetAttribute("style");

        Assert.Empty(cut.FindAll("fluent-menu"));
        Assert.Equal(MobileNavMenu.MobileNavMenuId, menu.Id);
        Assert.Equal(cut.FindAll("fluent-menu-item").Count, menu.QuerySelectorAll(":scope > fluent-menu-item").Length);
        Assert.Equal(cut.FindAll("fluent-menu-item").Count - 1, cut.FindAll("fluent-divider").Count);
        Assert.Equal("fluent-menu-item", menu.Children[0].LocalName);
        Assert.Equal("fluent-menu-item", menu.Children[menu.Children.Length - 1].LocalName);

        Assert.Contains("max-height: calc(100dvh - var(--mobile-header-height) - var(--mobile-nav-menu-offset))", style);
        Assert.DoesNotContain("height: 100vh", style);
        Assert.Contains("margin-top: var(--mobile-nav-menu-offset)", style);
        Assert.Contains("overflow-y: auto", style);
        Assert.Contains("padding-block: var(--mobile-nav-menu-focus-padding)", style);
        Assert.Contains("scroll-padding-block: var(--mobile-nav-menu-focus-padding)", style);
        Assert.Contains("mobile-nav-menu", menu.ClassList);
    }

    [Fact]
    public void Render_OpenMenu_InitializesKeyboardNavigationWithComponentReferenceAndMenuId()
    {
        _ = RenderMobileNavMenu(DashboardUrls.HomeUrl());

        var invocation = Assert.Single(JSInterop.Invocations, i => i.Identifier == "initializeMobileNavMenuKeyboardNavigation");
        Assert.Collection(
            invocation.Arguments,
            argument => Assert.IsAssignableFrom<DotNetObjectReference<MobileNavMenu>>(argument),
            argument => Assert.Equal(MobileNavMenu.MobileNavMenuId, argument));
    }

    [Fact]
    public async Task CloseMobileNavMenuFromFocusLossAsync_ClosesMenuWithoutRestoringFocus()
    {
        var closeNavMenuCalled = false;
        var cut = RenderMobileNavMenu(DashboardUrls.HomeUrl(), () => closeNavMenuCalled = true, isNavMenuOpen: false);

        await cut.InvokeAsync(cut.Instance.CloseMobileNavMenuFromFocusLossAsync);

        Assert.True(closeNavMenuCalled);
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "focusElement");
    }

    [Fact]
    public async Task CloseMobileNavMenuFromKeyboardAsync_ClosesMenuAndRestoresFocus()
    {
        JSInterop.SetupVoid("focusElement", _ => true).SetVoidResult();
        var closeNavMenuCalled = false;
        var cut = RenderMobileNavMenu(DashboardUrls.HomeUrl(), () => closeNavMenuCalled = true, isNavMenuOpen: false);

        await cut.InvokeAsync(cut.Instance.CloseMobileNavMenuFromKeyboardAsync);

        Assert.True(closeNavMenuCalled);
        var invocation = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "focusElement");
        var argument = Assert.Single(invocation.Arguments);
        Assert.Equal(MainLayout.NavigationButtonId, argument);
    }

    private IRenderedComponent<MobileNavMenu> RenderMobileNavMenu(string currentUrl, Action? closeNavMenu = null, bool isNavMenuOpen = true, bool hasResourceTerminals = false, bool hasResourceService = true, bool hasParameters = false)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(this);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient(isEnabled: hasResourceService));
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentMenu(this);
        FluentUISetupHelpers.SetupFluentDivider(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        LayoutSetupHelpers.SetupMobileNavMenuKeyboardNavigation(this);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(currentUrl);

        return Render<MobileNavMenu>(builder =>
        {
            builder.Add(p => p.IsNavMenuOpen, isNavMenuOpen);
            builder.Add(p => p.HasResourceTerminals, hasResourceTerminals);
            builder.Add(p => p.HasParameters, hasParameters);
            builder.Add(p => p.CloseNavMenu, closeNavMenu ?? (() => { }));
            builder.Add(p => p.LaunchHelpAsync, () => Task.CompletedTask);
            builder.Add(p => p.LaunchAIAgentsAsync, () => Task.CompletedTask);
            builder.Add(p => p.IsAgentHelpEnabled, false);
            builder.Add(p => p.LaunchNotificationsAsync, () => Task.CompletedTask);
            builder.Add(p => p.LaunchSettingsAsync, () => Task.CompletedTask);
        });
    }

    private static void AssertMenuItemIsActive(IRenderedComponent<MobileNavMenu> cut, string expectedText)
    {
        var currentItem = Assert.Single(cut.FindAll("""fluent-menu-item[aria-current="page"]"""));

        Assert.Contains(expectedText, currentItem.TextContent);
        Assert.True(currentItem.ClassList.Contains("mobile-nav-menu-item-active"));

        // The active item swaps to the filled icon variant and tags the slot wrapper
        // so the selected state has a non-color cue.
        var activeIconSlot = Assert.Single(currentItem.QuerySelectorAll(".mobile-nav-menu-icon-active"));
        Assert.Equal("start", activeIconSlot.GetAttribute("slot"));
        Assert.NotEmpty(activeIconSlot.QuerySelectorAll("svg"));

        var inactiveItems = cut.FindAll("fluent-menu-item")
            .Where(item => item.GetAttribute("aria-current") != "page")
            .ToList();
        Assert.NotEmpty(inactiveItems);
        Assert.All(inactiveItems, item =>
        {
            Assert.False(item.ClassList.Contains("mobile-nav-menu-item-active"));
            Assert.Empty(item.QuerySelectorAll(".mobile-nav-menu-icon-active"));
        });
    }
}
