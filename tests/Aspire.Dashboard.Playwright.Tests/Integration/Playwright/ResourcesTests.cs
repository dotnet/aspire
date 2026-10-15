// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.Dashboard.Resources;
using Aspire.TestUtilities;
using Aspire.Tests.Shared.DashboardModel;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Playwright;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[RequiresFeature(TestFeature.Playwright)]
public class ResourcesTests : PlaywrightTestsBase<ResourcesTests.ResourcesDashboardServerFixture>
{
    public ResourcesTests(ResourcesDashboardServerFixture dashboardServerFixture)
        : base(dashboardServerFixture)
    {
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ViewOptionsMenu_ReportsExpandedState()
    {
        await RunTestAsync(async page =>
        {
            await GoToParametersAndWaitForDataGridLoadAsync(page).DefaultTimeout();

            var viewOptionsButton = page.Locator(
                $"fluent-button[title='{Dashboard.Resources.Resources.ResourcesChangeViewOptions}']");
            await Assertions.Expect(viewOptionsButton).ToHaveAttributeAsync("aria-expanded", "false");

            await viewOptionsButton.ClickAsync();
            await Assertions.Expect(viewOptionsButton).ToHaveAttributeAsync("aria-expanded", "true");

            await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = Dashboard.Resources.Resources.ResourceCollapseAllChildren, Exact = true })).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = Dashboard.Resources.Resources.ResourceExpandAllChildren, Exact = true })).ToHaveCountAsync(0);
            var showResourceTypes = page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = Dashboard.Resources.Resources.ResourcesShowTypes, Exact = true });
            await showResourceTypes.ClickAsync();
            await Assertions.Expect(viewOptionsButton).ToHaveAttributeAsync("aria-expanded", "false");
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task GridActionButtons_UseCompactMinimumWidth()
    {
        await RunTestAsync(async page =>
        {
            await GoToParametersAndWaitForDataGridLoadAsync(page).DefaultTimeout();

            var values = await page.Locator(".grid-action-container fluent-button").EvaluateAllAsync<int[]>(
                """
                buttons => [
                    buttons.length,
                    buttons.filter(button => getComputedStyle(button).minWidth !== '32px').length
                ]
                """);

            Assert.True(values[0] > 0);
            Assert.Equal(0, values[1]);
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task NameColumn_SortsParameters()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/parameters");

            var resourceNames = page.Locator(".main-grid .resource-row .resource-name-text");
            await Assertions.Expect(resourceNames).ToHaveCountAsync(2);
            await Assertions.Expect(resourceNames.Nth(0)).ToContainTextAsync("alpha-param");
            await Assertions.Expect(resourceNames.Nth(1)).ToContainTextAsync("zeta-param");

            var nameHeader = page.Locator(".main-grid th[col-index='1']");
            var sortItem = page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = ControlsStrings.FluentDataGridHeaderCellSortButtonText, Exact = true });
            var nameHeaderButton = nameHeader.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = ControlsStrings.NameColumnHeader, Exact = true });

            await nameHeaderButton.ClickAsync();
            await sortItem.ClickAsync();
            await Assertions.Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "ascending");
            await Assertions.Expect(resourceNames.Nth(0)).ToContainTextAsync("alpha-param");

            await nameHeaderButton.ClickAsync();
            await page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = ControlsStrings.FluentDataGridHeaderCellSortAscendingButtonText, Exact = true }).ClickAsync();
            await Assertions.Expect(nameHeader).ToHaveAttributeAsync("aria-sort", "descending");
            await Assertions.Expect(resourceNames.Nth(0)).ToContainTextAsync("zeta-param");
            await Assertions.Expect(resourceNames.Nth(1)).ToContainTextAsync("alpha-param");
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourcePane_MobileDrawerSelectsResource()
    {
        await RunTestAsync(async page =>
        {
            await page.SetViewportSizeAsync(360, 720);
            await page.GotoAsync("/resources");

            var layout = page.Locator(".resources-layout");
            await Assertions.Expect(layout).Not.ToHaveClassAsync(new Regex("drawer-open"));

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Dashboard.Resources.Layout.ResourcePaneOpen, Exact = true }).ClickAsync();
            await Assertions.Expect(layout).ToHaveClassAsync(new Regex("drawer-open"));

            var resourceLink = page.Locator(".resource-pane").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "TestResource" });
            var linkBounds = await resourceLink.BoundingBoxAsync();
            Assert.NotNull(linkBounds);
            Assert.True(linkBounds.X >= 0 && linkBounds.X + linkBounds.Width <= 360, $"The resource link should fit inside the viewport, but it spanned {linkBounds.X} to {linkBounds.X + linkBounds.Width}.");

            await resourceLink.ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/resources/TestResource");
            await Assertions.Expect(layout).Not.ToHaveClassAsync(new Regex("drawer-open"));
            await Assertions.Expect(page.Locator(".resource-title")).ToHaveTextAsync("TestResource");
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task GraphView_SwitchesWithoutReloadOrLayoutCollapse()
    {
        await RunTestAsync(async page =>
        {
            await PlaywrightFixture.GoToHomeAndWaitForDataGridLoad(page).DefaultTimeout();

            var navigationCount = await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length");

            var graphLink = page.Locator(".main-rail").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = Dashboard.Resources.Layout.NavMenuGraphTab, Exact = true });
            await graphLink.ClickAsync();
            await Assertions.Expect(graphLink).ToHaveAttributeAsync("aria-current", "page");

            var graphContainer = page.Locator("#resourcesGraphContainer");
            await Assertions.Expect(graphContainer).ToBeVisibleAsync();

            var graphWidth = await graphContainer.EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            Assert.True(graphWidth > 100, $"The resource graph should fill the page content area, but its width was {graphWidth}px.");

            var navigationCountAfterSwitch = await page.EvaluateAsync<int>("() => performance.getEntriesByType('navigation').length");
            Assert.Equal(navigationCount, navigationCountAfterSwitch);
            await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourceGraphCog_IsKeyboardAccessibleAndDoesNotDragNode()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/graph");

            var node = page.Locator(".resource-group[resource-name='TestResource']");
            await Assertions.Expect(node).ToBeVisibleAsync();
            await node.HoverAsync();

            var resourceActionsLabel = string.Format(
                Dashboard.Resources.Resources.ResourcesGraphResourceActionsButton,
                "TestResource");
            var otherResourceActionsLabel = string.Format(
                Dashboard.Resources.Resources.ResourcesGraphResourceActionsButton,
                "apigateway");
            var cog = node.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions
                {
                    Name = resourceActionsLabel,
                    Exact = true
                });
            await Assertions.Expect(cog).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions
                {
                    Name = otherResourceActionsLabel,
                    Exact = true
                })).ToHaveCountAsync(1);

            // Attempt a large drag from the cog. D3's drag behavior is attached to the ancestor
            // resource group, so this verifies the cog stops the initiating pointer event.
            await WaitForPositionToStabilizeAsync(node);
            var nodeBoundsBefore = await node.BoundingBoxAsync();
            var cogBounds = await cog.BoundingBoxAsync();
            Assert.NotNull(nodeBoundsBefore);
            Assert.NotNull(cogBounds);

            await page.Mouse.MoveAsync(
                cogBounds.X + cogBounds.Width / 2,
                cogBounds.Y + cogBounds.Height / 2);
            await page.Mouse.DownAsync();
            Assert.Equal(
                "none",
                await cog.EvaluateAsync<string>("element => getComputedStyle(element).outlineStyle"));
            await page.Mouse.MoveAsync(
                cogBounds.X + cogBounds.Width / 2 + 80,
                cogBounds.Y + cogBounds.Height / 2 + 80,
                new MouseMoveOptions { Steps = 5 });
            await page.Mouse.UpAsync();

            var nodeBoundsAfter = await node.BoundingBoxAsync();
            Assert.NotNull(nodeBoundsAfter);
            Assert.InRange(Math.Abs(nodeBoundsAfter.X - nodeBoundsBefore.X), 0, 5);
            Assert.InRange(Math.Abs(nodeBoundsAfter.Y - nodeBoundsBefore.Y), 0, 5);
            Assert.False((await node.GetAttributeAsync("class"))?.Split(' ').Contains("resource-group-selected"));

            var menu = page.Locator(".aspire-menu-container:has(#resource-context-menu-header) fluent-menu-list");
            await Assertions.Expect(menu).ToBeHiddenAsync();

            await node.HoverAsync();
            await cog.ClickAsync();
            await Assertions.Expect(menu).ToBeVisibleAsync();
            await Assertions.Expect(cog).ToHaveAttributeAsync("aria-expanded", "true");
            Assert.False((await node.GetAttributeAsync("class"))?.Split(' ').Contains("resource-group-selected"));

            await menu.GetByRole(AriaRole.Menuitem).First.FocusAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(menu).ToBeHiddenAsync();
            await Assertions.Expect(cog).ToHaveAttributeAsync("aria-expanded", "false");

            await node.HoverAsync();
            await cog.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            await Assertions.Expect(menu).ToBeVisibleAsync();
            await Assertions.Expect(cog).ToHaveAttributeAsync("aria-haspopup", "menu");
            await Assertions.Expect(cog).ToHaveAttributeAsync("aria-expanded", "true");
            var header = menu.Locator(".aspire-menu-header");
            await Assertions.Expect(header.Locator(".aspire-menu-header-text")).ToHaveTextAsync("TestResource");
            var headerBounds = await header.BoundingBoxAsync();
            Assert.NotNull(headerBounds);
            Assert.InRange(headerBounds.Height, 35, 37);

            await menu.GetByRole(AriaRole.Menuitem).First.FocusAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(menu).ToBeHiddenAsync();
            await Assertions.Expect(cog).ToHaveAttributeAsync("aria-expanded", "false");
            await Assertions.Expect(cog).ToBeFocusedAsync();

            await page.Keyboard.PressAsync("Enter");
            await page.GetByRole(
                AriaRole.Menuitem,
                new PageGetByRoleOptions
                {
                    Name = ControlsStrings.ActionViewDetailsText,
                    Exact = true
                }).ClickAsync();

            // Non-parameter resources open in the Resources view rather than a graph side panel.
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/resources/TestResource");
            await Assertions.Expect(page.Locator(".resource-title")).ToHaveTextAsync("TestResource");
        });
    }

    private static async Task WaitForPositionToStabilizeAsync(ILocator locator)
    {
        await locator.EvaluateAsync(
            """
            element => new Promise(resolve => {
                let previousBounds;
                let stableFrames = 0;

                const checkPosition = () => {
                    const bounds = element.getBoundingClientRect();
                    if (previousBounds &&
                        Math.abs(bounds.x - previousBounds.x) <= 0.5 &&
                        Math.abs(bounds.y - previousBounds.y) <= 0.5) {
                        stableFrames++;
                    } else {
                        stableFrames = 0;
                    }

                    if (stableFrames >= 3) {
                        resolve();
                        return;
                    }

                    previousBounds = bounds;
                    requestAnimationFrame(checkPosition);
                };

                requestAnimationFrame(checkPosition);
            })
            """).DefaultTimeout();
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task GraphNode_RightClickOpensMenuAtCursorWithoutOverlay()
    {
        await RunTestAsync(async page =>
        {
            await page.GotoAsync("/graph");

            var node = page.Locator(".resource-node").First;
            await Assertions.Expect(node).ToBeVisibleAsync();
            var nodeBounds = await node.BoundingBoxAsync();
            Assert.NotNull(nodeBounds);

            var cursorX = nodeBounds.X + nodeBounds.Width / 2;
            var cursorY = nodeBounds.Y + nodeBounds.Height / 2;
            await node.Locator("xpath=..").EvaluateAsync(
                """
                element => {
                    const bounds = element.querySelector('.resource-node').getBoundingClientRect();
                    element.dispatchEvent(new MouseEvent('contextmenu', {
                        bubbles: true,
                        button: 2,
                        clientX: Math.round(bounds.left + bounds.width / 2),
                        clientY: Math.round(bounds.top + bounds.height / 2)
                    }));
                }
                """);

            var menu = page.Locator(".aspire-menu-container:has(#resource-context-menu-header) fluent-menu-list");
            await Assertions.Expect(menu).ToBeVisibleAsync();
            var menuBounds = await menu.BoundingBoxAsync();
            Assert.NotNull(menuBounds);

            Assert.InRange(Math.Abs(menuBounds.X - cursorX), 0, 2);
            Assert.InRange(Math.Abs(menuBounds.Y - cursorY), 0, 2);

            var visibleBlockingIndicators = await page.Locator("fluent-overlay, .aspire-progress-ring").EvaluateAllAsync<int>(
                "elements => elements.filter(element => element.getBoundingClientRect().width > 0 && element.getBoundingClientRect().height > 0).length");
            Assert.Equal(0, visibleBlockingIndicators);

            await menu.GetByRole(AriaRole.Menuitem).First.FocusAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(menu).ToBeHiddenAsync();

            await node.Locator("xpath=..").EvaluateAsync(
                """
                element => {
                    const bounds = element.querySelector('.resource-node').getBoundingClientRect();
                    element.dispatchEvent(new MouseEvent('contextmenu', {
                        bubbles: true,
                        button: 2,
                        clientX: Math.round(bounds.left + bounds.width / 2),
                        clientY: Math.round(bounds.top + bounds.height / 2)
                    }));
                }
                """);

            await Assertions.Expect(menu).ToBeVisibleAsync();
        });
    }

    public sealed class ResourcesDashboardServerFixture : DashboardServerFixture
    {
        protected override IReadOnlyList<ResourceViewModel> Resources =>
        [
            ModelTestHelpers.CreateResource(
                resourceName: "apigateway",
                resourceType: KnownResourceTypes.Project,
                state: KnownResourceState.Running),
            ModelTestHelpers.CreateResource(
                resourceName: "basketcache",
                resourceType: KnownResourceTypes.Container,
                state: KnownResourceState.Running),
            ModelTestHelpers.CreateResource(
                resourceName: "hidden-resource",
                resourceType: KnownResourceTypes.Container,
                state: KnownResourceState.Running,
                hidden: true),
            ModelTestHelpers.CreateResource(
                resourceName: "TestResource",
                resourceType: KnownResourceTypes.Project,
                state: KnownResourceState.Running,
                urls:
                [
                    new UrlViewModel("http", new Uri("about:blank#resource-url"), isInternal: false, isInactive: false, UrlDisplayPropertiesViewModel.Empty)
                ]),
            ModelTestHelpers.CreateResource(
                resourceName: "zeta-param",
                resourceType: KnownResourceTypes.Parameter,
                state: KnownResourceState.Running),
            ModelTestHelpers.CreateResource(
                resourceName: "alpha-param",
                resourceType: KnownResourceTypes.Parameter,
                state: KnownResourceState.Running)
        ];
    }

    private static async Task GoToParametersAndWaitForDataGridLoadAsync(IPage page)
    {
        await page.GotoAsync("/parameters");
        await Assertions.Expect(page.Locator(".main-grid .resource-row").First).ToBeVisibleAsync();
    }
}
