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
    public async Task MainNavigation_NavigatesBetweenSectionsAndMarksActiveItem()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/");

            // The root URL opens the resources, with all resources selected.
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath.StartsWith("/resources", StringComparison.Ordinal));
            var nav = page.Locator(".desktop-nav-menu");
            await AssertActiveItemAsync(Resources.Layout.NavMenuResourcesTab);

            var navigationCount = await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length");

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuResourcesTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath.StartsWith("/resources", StringComparison.Ordinal));
            await AssertActiveItemAsync(Resources.Layout.NavMenuResourcesTab);

            await nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuParametersTab, Exact = true }).ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/parameters");
            await AssertActiveItemAsync(Resources.Layout.NavMenuParametersTab);
            await Assertions.Expect(nav.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuGraphTab, Exact = true })).ToHaveCountAsync(0);

            // Navigation links are handled by Blazor's router, so switching sections doesn't reload the document.
            Assert.Equal(navigationCount, await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length"));

            async Task AssertActiveItemAsync(string expectedName)
            {
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
