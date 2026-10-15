// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.Dashboard.Utils;
using Aspire.TestUtilities;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[RequiresFeature(TestFeature.Playwright)]
public sealed class DesktopNavMenuTests : PlaywrightTestsBase<DashboardServerFixture>
{
    public DesktopNavMenuTests(DashboardServerFixture dashboardServerFixture)
        : base(dashboardServerFixture)
    {
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task MainRail_NavigatesBetweenSectionsAndMarksActiveItem()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/");

            var nav = page.Locator(".main-rail");
            await AssertActiveItemAsync(Resources.Layout.NavMenuHomeTab);

            var navigationCount = await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length");

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuResourcesTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath.StartsWith("/resources", StringComparison.Ordinal));
            await AssertActiveItemAsync(Resources.Layout.NavMenuResourcesTab);

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuParametersTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/parameters");
            await AssertActiveItemAsync(Resources.Layout.NavMenuParametersTab);

            // Parameters and Graph share a page component, so switching between them must update the displayed view.
            var gridContainer = page.Locator(".resources-grid-container");
            var graphContainer = page.Locator(".resource-graph-container");
            await Assertions.Expect(gridContainer).ToBeVisibleAsync();
            await Assertions.Expect(graphContainer).ToBeHiddenAsync();

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuGraphTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/graph");
            await AssertActiveItemAsync(Resources.Layout.NavMenuGraphTab);
            await Assertions.Expect(graphContainer).ToBeVisibleAsync();
            await Assertions.Expect(gridContainer).ToBeHiddenAsync();

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuParametersTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/parameters");
            await AssertActiveItemAsync(Resources.Layout.NavMenuParametersTab);
            await Assertions.Expect(gridContainer).ToBeVisibleAsync();
            await Assertions.Expect(graphContainer).ToBeHiddenAsync();

            // Rail links are handled by Blazor's router, so switching sections doesn't reload the document.
            Assert.Equal(navigationCount, await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length"));

            async Task AssertActiveItemAsync(string expectedName)
            {
                // Rail items are icon-only, so their label is the accessible name rather than the text content.
                var activeItem = nav.Locator("a[aria-current='page']");
                await Assertions.Expect(activeItem).ToHaveCountAsync(1);
                await Assertions.Expect(activeItem).ToHaveAccessibleNameAsync(expectedName);
            }
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourcePaneToggle_ChangesLayoutAndPersistsCollapsedState()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/resources");
            await page.EvaluateAsync($"localStorage.setItem('{BrowserStorageKeys.ResourcePaneCollapsed}', 'false')");
            await page.ReloadAsync();

            var layout = page.Locator(".resources-layout");
            await Assertions.Expect(layout).Not.ToHaveClassAsync(new Regex("pane-collapsed"));

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Resources.Layout.ResourcePaneCollapse, Exact = true }).ClickAsync();
            await Assertions.Expect(layout).ToHaveClassAsync(new Regex("pane-collapsed"));

            await page.ReloadAsync();
            await Assertions.Expect(layout).ToHaveClassAsync(new Regex("pane-collapsed"));

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Resources.Layout.ResourcePaneExpand, Exact = true }).ClickAsync();
            await Assertions.Expect(layout).Not.ToHaveClassAsync(new Regex("pane-collapsed"));
        });
    }
}
