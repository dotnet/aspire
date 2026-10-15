// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;
using Aspire.Dashboard.Utils;
using Aspire.TestUtilities;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

[RequiresFeature(TestFeature.Playwright)]
public sealed class ResourcePaneInteractionTests : PlaywrightTestsBase<DashboardServerFixture>
{
    public ResourcePaneInteractionTests(DashboardServerFixture dashboardServerFixture)
        : base(dashboardServerFixture)
    {
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task TelemetryOnlyResourceSelection_KeepsCurrentTabAndSupportsConsecutiveChanges()
    {
        var repository = DashboardServerFixture.DashboardApp.Services.GetRequiredService<ITelemetryRepositoryWriter>();
        await repository.AddLogsAsync(new AddContext(), new RepeatedField<ResourceLogs>
        {
            CreateResourceLogs("resource-a"),
            CreateResourceLogs("resource-b")
        });

        await RunTestAsync(async page =>
        {
            await page.GotoAsync(DashboardUrls.TelemetrySourcesUrl());

            var pane = page.Locator(".resource-pane");

            await SelectResourceAsync("resource-a");
            await SelectResourceAsync("resource-b");

            async Task SelectResourceAsync(string resourceName)
            {
                var link = pane.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = resourceName });
                await link.ClickAsync();

                await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/structuredlogs/resource/{resourceName}\\?pane=telemetry$"));
                await Assertions.Expect(page.Locator("h1.resource-title")).ToHaveTextAsync(resourceName);
                await Assertions.Expect(link).ToHaveAttributeAsync("aria-current", "page");
            }
        });

    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task AllResourcesRow_ClearsSelectionAndListsResourcesInOverview()
    {
        const string resourceName = "TestResource";

        await RunTestAsync(async page =>
        {
            await page.SetViewportSizeAsync(1280, 900);
            await page.GotoAsync($"/resources/{resourceName}");

            var pane = page.Locator(".resource-pane");
            var title = page.Locator("h1.resource-title");
            var allResourcesLink = pane.Locator(".resource-row-all a");
            await Assertions.Expect(title).ToHaveTextAsync(resourceName);
            await Assertions.Expect(allResourcesLink).Not.ToHaveAttributeAsync("aria-current", "page");

            await allResourcesLink.ClickAsync();
            await Assertions.Expect(title).ToHaveTextAsync(Resources.Layout.ResourceHeaderAllResources);
            await Assertions.Expect(allResourcesLink).ToHaveAttributeAsync("aria-current", "page");
            await Assertions.Expect(pane.Locator("a[aria-current]")).ToHaveCountAsync(1);

            var overviewCards = page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = Resources.Layout.ResourceHeaderAllResources });
            await Assertions.Expect(overviewCards.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = resourceName, Exact = true })).ToBeVisibleAsync();

            // Switching tabs keeps all resources selected, including when returning to the overview.
            var tabs = page.Locator(".resource-tabs");
            await tabs.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuConsoleLogsTab }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/consolelogs$"));
            await Assertions.Expect(tabs.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = Resources.Layout.NavMenuConsoleLogsTab })).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(title).ToHaveTextAsync(Resources.Layout.ResourceHeaderAllResources);

            await tabs.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = Resources.Layout.ResourceTabOverview }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/resources$"));
            await Assertions.Expect(overviewCards).ToBeVisibleAsync();
            await Assertions.Expect(allResourcesLink).ToHaveAttributeAsync("aria-current", "page");

            // The cleared selection is remembered when entering the resources view again.
            await page.GotoAsync("/resources");
            await Assertions.Expect(overviewCards).ToBeVisibleAsync();
            await Assertions.Expect(title).ToHaveTextAsync(Resources.Layout.ResourceHeaderAllResources);
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourceRowModifierClicks_SelectSeveralResources()
    {
        var repository = DashboardServerFixture.DashboardApp.Services.GetRequiredService<ITelemetryRepositoryWriter>();
        await repository.AddLogsAsync(new AddContext(), new RepeatedField<ResourceLogs>
        {
            CreateResourceLogs("multi-a"),
            CreateResourceLogs("multi-b"),
            CreateResourceLogs("multi-c")
        });

        await RunTestAsync(async page =>
        {
            await page.SetViewportSizeAsync(1280, 900);
            await page.GotoAsync(DashboardUrls.AddTelemetryPane(DashboardUrls.StructuredLogsUrl(resource: "multi-a")));

            var pane = page.Locator(".resource-pane");
            var title = page.Locator("h1.resource-title");
            await Assertions.Expect(title).ToHaveTextAsync("multi-a");

            await GetRowLink("multi-c").ClickAsync(new LocatorClickOptions { Modifiers = [KeyboardModifier.ControlOrMeta] });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/structuredlogs\?resource=multi-a&resource=multi-c&pane=telemetry$"));
            await Assertions.Expect(title).ToHaveTextAsync(string.Format(CultureInfo.CurrentCulture, Resources.Layout.ResourceHeaderSelectedResources, 2));
            await Assertions.Expect(pane.Locator("a[aria-current='true']")).ToHaveCountAsync(2);
            await Assertions.Expect(page.Locator(".resource-header-chip")).ToHaveTextAsync(["multi-a", "multi-c"]);

            // Shift+click selects the rows between the last clicked row (multi-c) and the clicked row.
            await GetRowLink("multi-a").ClickAsync(new LocatorClickOptions { Modifiers = [KeyboardModifier.Shift] });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/structuredlogs\?resource=multi-a&resource=multi-b&resource=multi-c&pane=telemetry$"));
            await Assertions.Expect(title).ToHaveTextAsync(string.Format(CultureInfo.CurrentCulture, Resources.Layout.ResourceHeaderSelectedResources, 3));

            await GetRowLink("multi-b").ClickAsync(new LocatorClickOptions { Modifiers = [KeyboardModifier.ControlOrMeta] });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/structuredlogs\?resource=multi-a&resource=multi-c&pane=telemetry$"));

            await GetRowLink("multi-b").ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/structuredlogs/resource/multi-b\?pane=telemetry$"));
            await Assertions.Expect(title).ToHaveTextAsync("multi-b");
            await Assertions.Expect(GetRowLink("multi-b")).ToHaveAttributeAsync("aria-current", "page");

            ILocator GetRowLink(string resourceName) => pane.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = resourceName });
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourcePaneResizer_ResizesAndPersistsWidth()
    {
        await RunTestAsync(async page =>
        {
            await page.SetViewportSizeAsync(1280, 900);
            await page.GotoAsync("/structuredlogs");
            await page.EvaluateAsync($"localStorage.removeItem('{BrowserStorageKeys.ResourcePaneWidth}')");
            await page.ReloadAsync();

            var pane = page.Locator(".resource-pane");
            var resizer = page.GetByRole(AriaRole.Separator, new PageGetByRoleOptions { Name = Resources.Layout.ResourcePaneResize });
            await Assertions.Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "290");

            var box = await resizer.BoundingBoxAsync();
            Assert.NotNull(box);
            var x = box.X + box.Width / 2;
            var y = box.Y + box.Height / 2;
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x + 60, y, new MouseMoveOptions { Steps = 5 });
            await page.Mouse.UpAsync();

            await Assertions.Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "350");
            Assert.Equal(350, await GetPaneWidthAsync());

            await resizer.FocusAsync();
            await page.Keyboard.PressAsync("ArrowLeft");
            await Assertions.Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "340");

            await AssertStoredWidthAsync("340");
            await page.ReloadAsync();
            await Assertions.Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "340");
            Assert.Equal(340, await GetPaneWidthAsync());

            await resizer.DblClickAsync();
            await Assertions.Expect(resizer).ToHaveAttributeAsync("aria-valuenow", "290");
            Assert.Equal(290, await GetPaneWidthAsync());

            async Task<int> GetPaneWidthAsync() => await pane.EvaluateAsync<int>("e => Math.round(e.getBoundingClientRect().width)");

            async Task AssertStoredWidthAsync(string expected)
            {
                await AsyncTestHelpers.AssertIsTrueRetryAsync(
                    async () => await page.EvaluateAsync<string?>($"localStorage.getItem('{BrowserStorageKeys.ResourcePaneWidth}')") == expected,
                    "Resource pane width wasn't persisted.");
            }
        });
    }

    [Fact]
    [OuterloopTest("Resource-intensive Playwright browser test")]
    public async Task ResourceTag_AddedFromOverviewGroupsResourceInResourcePane()
    {
        var resourceName = MockDashboardClient.TestResource1.DisplayName;
        const string tag = "Frontend team";

        await RunTestAsync(async page =>
        {
            await page.GotoAsync($"/resources/{resourceName}");

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = Resources.Resources.ResourceTagsAdd, Exact = true }).ClickAsync();
            var input = page.GetByRole(AriaRole.Combobox);
            await input.FillAsync("  frontend   team ");
            await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveTextAsync(string.Format(CultureInfo.CurrentCulture, Resources.Resources.ResourceTagsCreate, "frontend team"));
            await input.PressAsync("Enter");

            // The editor stays open with an empty input so several tags can be added in a row.
            await Assertions.Expect(page.Locator(".tag-chip-name")).ToHaveTextAsync("frontend team");
            await Assertions.Expect(input).ToBeFocusedAsync();
            await Assertions.Expect(input).ToHaveValueAsync(string.Empty);
            await input.PressAsync("Backspace");
            await Assertions.Expect(page.Locator(".tag-chip-name")).ToHaveCountAsync(0);
            await input.FillAsync(tag);
            await input.PressAsync("Enter");
            await input.PressAsync("Escape");
            await Assertions.Expect(page.Locator(".tag-chip-name")).ToHaveTextAsync(tag);

            var rail = page.Locator(".main-rail");
            await page.Locator("#resourcePaneGroupingButton").ClickAsync();
            await page.GetByRole(AriaRole.Menuitemradio, new PageGetByRoleOptions { Name = Resources.Layout.NavMenuTagsTab, Exact = true }).ClickAsync();

            var pane = page.Locator(".resource-pane");
            var tagGroup = pane.GetByRole(AriaRole.Group, new LocatorGetByRoleOptions { Name = tag });
            await Assertions.Expect(tagGroup.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = resourceName })).ToBeVisibleAsync();
            await Assertions.Expect(rail.Locator("a[aria-current='page']")).ToHaveAccessibleNameAsync(Resources.Layout.NavMenuResourcesTab);

            // Grouping is remembered when reloading a resource URL.
            await page.GotoAsync($"/resources/{resourceName}");
            await Assertions.Expect(tagGroup).ToBeVisibleAsync();

            await page.Locator("#resourcePaneGroupingButton").ClickAsync();
            await page.GetByRole(AriaRole.Menuitemradio, new PageGetByRoleOptions { Name = Resources.Layout.ResourcePaneGroupingNone, Exact = true }).ClickAsync();
            await Assertions.Expect(tagGroup).ToHaveCountAsync(0);
            await Assertions.Expect(pane.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = resourceName })).ToBeVisibleAsync();
            await Assertions.Expect(rail.Locator("a[aria-current='page']")).ToHaveAccessibleNameAsync(Resources.Layout.NavMenuResourcesTab);
        });
    }

    private static ResourceLogs CreateResourceLogs(string resourceName)
    {
        return new ResourceLogs
        {
            Resource = CreateResource(name: resourceName, instanceId: resourceName),
            ScopeLogs =
            {
                new ScopeLogs
                {
                    Scope = CreateScope(),
                    LogRecords = { CreateLogRecord(message: resourceName) }
                }
            }
        };
    }
}