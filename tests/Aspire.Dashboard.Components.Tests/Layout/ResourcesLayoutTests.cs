// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Dashboard.Utils;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Components.Tests.Layout;

[UseCulture("en-US")]
public class ResourcesLayoutTests : DashboardTestContext
{
    [Fact]
    public void AppHostTagsParticipateInGroupingWithoutUserTags()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var name = KnownProperties.Resource.TraitPrefix + KnownResourceTraits.DashboardTag;
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", properties: new()
                {
                    [name] = new(name, Value.ForList(Value.ForString("Backend"), Value.ForString("API")), false, null, 0, null, false)
                }),
                ModelTestHelpers.CreateResource("cache")
            ], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneGrouping ? (true, "Tags") : (false, null);
        Render<FluentToastProvider>();
        var cut = Render<ResourcesLayout>(p => p.AddCascadingValue(viewport));

        cut.WaitForAssertion(() => Assert.Equal(["Tags:API", "Tags:Backend", "Tags:"],
            cut.FindAll(".resource-tag-group").Select(g => g.GetAttribute("data-resource-group-key"))));
        Assert.Equal(["api", "api", "cache"], cut.FindAll(".resource-tag-group .resource-row-name").Select(n => n.TextContent.Trim()));
        Assert.Empty(Services.GetRequiredService<ResourceTagStore>().GetAllTags());
    }

    [Theory]
    [InlineData("Tags", "Tags:A new group")]
    [InlineData("ResourceType", "ResourceType:AAA")]
    [InlineData("State", "State:Building")]
    public async Task AppHost_GroupMovesPreserveMembershipAndModeSpecificOrderAndAppendNewGroups(string grouping, string newGroupKey)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Exited),
                ModelTestHelpers.CreateResource("worker", resourceType: "Executable")
            ], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneGrouping ? (true, grouping) : (false, null);
        var tags = Services.GetRequiredService<ResourceTagStore>();
        tags.AddTag("api", "Alpha");
        tags.AddTag("cache", "Beta");
        tags.AddTag("api", "Untagged");
        Render<FluentToastProvider>();
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        var store = Services.GetRequiredService<ResourceOrderStore>();
        var resourcePositions = store.GetPositions();
        var otherModes = new[] { "Tags", "ResourceType", "State" }.Where(mode => mode != grouping)
            .ToDictionary(mode => mode, store.GetGroupPositions);
        var originalKeys = GetKeys();
        var memberships = cut.FindAll(".resource-tag-group").ToDictionary(group => group.GetAttribute("data-resource-group-key")!,
            group => group.QuerySelectorAll(".resource-row-name").Select(name => name.TextContent.Trim()).ToArray());
        var movedKey = originalKeys[^1];
        var targetKey = originalKeys[0];
        Assert.Equal(originalKeys.Length, cut.FindAll("[data-resource-group-drag-handle]").Count);
        Assert.Equal("true", cut.Find(".resource-pane-list").GetAttribute("data-group-reorder-enabled"));
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync(grouping, movedKey, targetKey, after: false)));
        var expectedKeys = new[] { movedKey }.Concat(originalKeys[..^1]).ToArray();
        Assert.Equal(expectedKeys, GetKeys());
        Assert.All(memberships, pair => Assert.Equal(pair.Value,
            GetGroup(pair.Key).QuerySelectorAll(".resource-row-name").Select(name => name.TextContent.Trim())));
        Assert.Equal(resourcePositions, store.GetPositions());
        Assert.All(otherModes, pair => Assert.Equal(pair.Value, store.GetGroupPositions(pair.Key)));
        Assert.Equal(["Alpha", "Untagged"], tags.GetTags("api"));
        Assert.Equal(["Beta"], tags.GetTags("cache"));

        GetGroup(movedKey).QuerySelector(".resource-row-toggle")!.Click();
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync(grouping, movedKey, targetKey, after: true)));
        Assert.Empty(GetGroup(movedKey).QuerySelectorAll(".resource-row"));
        Assert.Equal("false", GetGroup(movedKey).QuerySelector(".resource-row-toggle")!.GetAttribute("aria-expanded"));
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync(grouping, movedKey, targetKey, after: false)));
        GetGroup(movedKey).QuerySelector(".resource-row-toggle")!.Click();

        var search = cut.FindComponent<FluentTextInput>();
        await cut.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("api"));
        if (grouping == "Tags")
        {
            tags.AddTag("api", "A new group");
        }
        else
        {
            await channel.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert,
                ModelTestHelpers.CreateResource("new-resource", state: KnownResourceState.Building, resourceType: "AAA"))]);
        }
        cut.WaitForAssertion(() => Assert.True(store.GetGroupPositions(grouping).ContainsKey(newGroupKey[(grouping.Length + 1)..])));
        await cut.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync(string.Empty));
        cut.WaitForAssertion(() => Assert.Equal(expectedKeys.Append(newGroupKey), GetKeys()));

        cut.Find("#resourcePaneGroupingButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneGroupingButton");
        var otherMode = otherModes.Keys.First();
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{otherMode}").OnClick!);
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync(grouping, movedKey, targetKey, after: false)));
        cut.Find("#resourcePaneGroupingButton").Click();
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{grouping}").OnClick!);
        Assert.Equal(expectedKeys.Append(newGroupKey), GetKeys());

        string[] GetKeys() => cut.FindAll(".resource-tag-group").Select(group => group.GetAttribute("data-resource-group-key")!).ToArray();
        AngleSharp.Dom.IElement GetGroup(string key) => cut.FindAll(".resource-tag-group").Single(group => group.GetAttribute("data-resource-group-key") == key);
    }

    [Theory]
    [InlineData("Search")]
    [InlineData("types")]
    [InlineData("states")]
    [InlineData("health")]
    public async Task AppHost_GroupReorderingRequiresClearSearchAndFilters(string filter)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Exited)
            ], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneGrouping ? (true, "State") : (false, null);
        Render<FluentToastProvider>();
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-resource-group-drag-handle]").Count));
        var positions = Services.GetRequiredService<ResourceOrderStore>().GetGroupPositions("State");
        Func<Task> reset;
        if (filter == "Search")
        {
            var search = cut.FindComponent<FluentTextInput>();
            await cut.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("a"));
            reset = () => search.Instance.ValueChanged.InvokeAsync(string.Empty);
        }
        else
        {
            cut.Find("#resourcePaneFilterButton").Click();
            var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
            var item = menu.Instance.Items.Single(item => item.Id == $"resource-filter-{filter}-0");
            await cut.InvokeAsync(() => item.OnCheckedChanged!(false));
            reset = () => item.OnCheckedChanged!(true);
        }

        Assert.Equal("false", cut.Find(".resource-pane-list").GetAttribute("data-group-reorder-enabled"));
        Assert.Empty(cut.FindAll("[data-resource-group-drag-handle]"));
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync("State", "State:Running", "State:Exited", after: false)));
        Assert.Equal(positions, Services.GetRequiredService<ResourceOrderStore>().GetGroupPositions("State"));
        await cut.InvokeAsync(reset);
        Assert.Equal(2, cut.FindAll("[data-resource-group-drag-handle]").Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AppHost_ResourceLinksHaveAccessibleMetadataInsteadOfHoverTooltips(bool collapsed)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Running, properties: new()
            {
                [KnownProperties.Container.Lifetime] = new(KnownProperties.Container.Lifetime, Value.ForString("Persistent"), false, null, 0, null, false)
            })], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneCollapsed ? (true, collapsed) : (false, null);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        Assert.Equal(
            [
                ("All resources", "1 resource", (string?)null),
                ("cache", $"Running; Container; {Resources.Columns.PersistentContainerIconTooltip}", null)
            ],
            cut.FindAll(".resource-row-link").Select(link => (
                link.GetAttribute("aria-label"), link.GetAttribute("aria-description"), link.GetAttribute("data-tooltip"))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Standalone_SourceLinksHaveAccessibleNamesInsteadOfHoverTooltips(bool collapsed)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: false));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneCollapsed ? (true, collapsed) : (false, null);
        await AddTelemetryAsync("api");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        Assert.Equal(
            [
                ("All sources", "1 source", (string?)null),
                ("api", Resources.Layout.ResourcePaneTelemetryOnly, null)
            ],
            cut.FindAll(".resource-row-link").Select(link => (
                link.GetAttribute("aria-label"), link.GetAttribute("aria-description"), link.GetAttribute("data-tooltip"))));
    }

    [Fact]
    public async Task AppHost_ReorderingPreservesHierarchyAndAppendsNewSiblings()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("root"),
                ModelTestHelpers.CreateResource("worker"),
                ModelTestHelpers.CreateResource("a", properties: new()
                {
                    [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
                }),
                ModelTestHelpers.CreateResource("b", properties: new()
                {
                    [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
                })
            ], resourceChannelProvider: () => channel));
        var store = Services.GetRequiredService<ResourceOrderStore>();
        store.EnsureResources(["worker_0", "root_0", "b_0", "a_0"]);
        var toasts = Render<FluentToastProvider>();
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(["worker", "root", "b", "a"], GetNames()));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("root", "worker", after: false)));
        Assert.Equal(["root", "b", "a", "worker"], GetNames());
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("a", "b", after: false)));
        Assert.Equal(["root", "a", "b", "worker"], GetNames());
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("a", "worker", after: true)));
        Assert.Equal(Resources.Layout.ResourcePaneReorderUnavailable, Assert.Single(toasts.FindComponents<FluentToast>()).Instance.Title);
        Assert.Equal(["root", "a", "b", "worker"], GetNames());

        cut.Find("[data-resource-name='root'] .resource-row-toggle").Click();
        Assert.Equal(["root", "worker"], GetNames());
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("worker", "root", after: false)));
        cut.Find("[data-resource-name='root'] .resource-row-toggle").Click();
        Assert.Equal(["worker", "root", "a", "b"], GetNames());

        await channel.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("0", properties: new()
        {
            [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
        }))]);
        cut.WaitForAssertion(() => Assert.Equal(["worker", "root", "a", "b", "0"], GetNames()));

        Assert.Equal(["worker_0", "root_0", "a_0", "b_0", "0_0"],
            store.GetPositions().OrderBy(pair => pair.Value).Select(pair => pair.Key));

        string?[] GetNames() => cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name")).ToArray();
    }

    [Fact]
    public async Task AppHost_NewResourcesAppendAndRunSpecificNamesKeepTheirPosition()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var originalApi = ModelTestHelpers.CreateResource("api-first-run", displayName: "api");
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [originalApi, ModelTestHelpers.CreateResource("cache")], resourceChannelProvider: () => channel));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".resource-row-drag-handle").Count));
        Assert.True(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("cache", originalApi.Name, after: false)));

        await channel.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("aaa"))]);
        cut.WaitForAssertion(() => Assert.Equal(["cache", "api-first-run", "aaa"],
            cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name"))));
        await channel.Writer.WriteAsync([
            new(ResourceViewModelChangeType.Delete, originalApi),
            new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("api-second-run", displayName: "api"))
        ]);
        cut.WaitForAssertion(() => Assert.Equal(["cache", "api-second-run", "aaa"],
            cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name"))));
    }

    [Theory]
    [InlineData("Search")]
    [InlineData("types")]
    [InlineData("states")]
    [InlineData("health")]
    [InlineData("Tags")]
    [InlineData("ResourceType")]
    [InlineData("State")]
    public async Task AppHost_ReorderingRequiresNoSearchFiltersOrGrouping(string mode)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Exited)
            ], resourceChannelProvider: () => channel));
        Render<FluentToastProvider>();
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".resource-row-drag-handle").Count));
        var positions = Services.GetRequiredService<ResourceOrderStore>().GetPositions();
        Func<Task> reset;
        if (mode == "Search")
        {
            var input = cut.FindComponent<FluentTextInput>();
            await cut.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync("a"));
            reset = () => input.Instance.ValueChanged.InvokeAsync(string.Empty);
        }
        else if (mode is "types" or "states" or "health")
        {
            cut.Find("#resourcePaneFilterButton").Click();
            var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
            var item = menu.Instance.Items.Single(item => item.Id == $"resource-filter-{mode}-0");
            await cut.InvokeAsync(() => item.OnCheckedChanged!(false));
            reset = () => item.OnCheckedChanged!(true);
        }
        else
        {
            cut.Find("#resourcePaneGroupingButton").Click();
            var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneGroupingButton");
            await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{mode}").OnClick!);
            reset = menu.Instance.Items.Single(item => item.Id == "resource-grouping-None").OnClick!;
        }

        Assert.Empty(cut.FindAll(".resource-row-drag-handle"));
        Assert.Equal("false", cut.Find(".resource-pane-list").GetAttribute("data-reorder-enabled"));
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ReorderResourceAsync("api", "cache", after: false)));
        Assert.Equal(positions, Services.GetRequiredService<ResourceOrderStore>().GetPositions());
        await cut.InvokeAsync(reset);
        Assert.Equal(2, cut.FindAll(".resource-row-drag-handle").Count);
        Assert.Equal("true", cut.Find(".resource-pane-list").GetAttribute("data-reorder-enabled"));
    }

    [Theory]
    [InlineData("Persistent", 1)]
    [InlineData("Session", 0)]
    [InlineData(null, 0)]
    public void AppHost_PersistentContainerPinFollowsStateAndType(string? lifetime, int expectedPins)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Running, properties: lifetime is null ? [] : new()
            {
                [KnownProperties.Container.Lifetime] = new(KnownProperties.Container.Lifetime, Value.ForString(lifetime), false, null, 0, null, false)
            })], resourceChannelProvider: () => channel));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        Assert.Equal(expectedPins, cut.FindAll(".resource-row-type + .persistent-container-icon").Count);
        if (expectedPins == 1)
        {
            Assert.Equal(Resources.Columns.PersistentContainerIconTooltip, cut.Find(".persistent-container-icon").GetAttribute("aria-label"));
        }
    }

    [Theory]
    [InlineData("Tags", new[] { "Backend", "Shared", "Untagged" })]
    [InlineData("ResourceType", new[] { "Container", "Project" })]
    [InlineData("State", new[] { "Exited", "Running", "Unknown" })]
    public async Task AppHost_GroupingSupportsSelectionCollapseSearchAndReset(string grouping, string[] expectedGroups)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("worker", state: KnownResourceState.Exited, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", resourceType: "Container")
            ], resourceChannelProvider: () => channel));
        var tags = Services.GetRequiredService<ResourceTagStore>();
        tags.AddTag("api", "Backend");
        tags.AddTag("worker", "Backend");
        tags.AddTag("api", "Shared");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.StructuredLogsUrl("api"));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        Assert.Empty(cut.FindAll(".resource-tag-group"));

        cut.Find("#resourcePaneGroupingButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneGroupingButton");
        Assert.Equal(["None", "Tags", "Resource type", "State"], menu.Instance.Items.Select(item => item.Text));
        Assert.All(menu.Instance.Items, item => Assert.Equal(MenuItemRole.Radio, item.Role));
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{grouping}").OnClick!);
        Assert.Equal(expectedGroups, cut.FindAll(".resource-tag-group").Select(group => group.GetAttribute("aria-label")));
        Assert.Equal("http://localhost/structuredlogs/resource/api", navigation.Uri);
        Assert.Equal("3 resources", cut.Find(".resource-pane-foot > span").TextContent.Trim());

        var group = cut.FindAll(".resource-tag-group")[0];
        var groupName = group.GetAttribute("aria-label");
        var resourceNames = group.QuerySelectorAll(".resource-row-name").Select(name => name.TextContent.Trim()).ToList();
        Assert.NotEmpty(resourceNames);
        group.QuerySelector(".resource-row-toggle")!.Click();
        Assert.Empty(cut.FindAll(".resource-tag-group")[0].QuerySelectorAll(".resource-row"));
        cut.FindAll(".resource-tag-group")[0].QuerySelector(".resource-row-toggle")!.Click();
        Assert.Equal(resourceNames, cut.FindAll(".resource-tag-group")[0].QuerySelectorAll(".resource-row-name").Select(name => name.TextContent.Trim()));

        cut.FindAll(".resource-tag-group")[0].QuerySelector(".resource-tag-link")!.Click();
        cut.Render();
        Assert.Equal(resourceNames, cut.Instance.SelectedResourceNames);
        await cut.FindComponent<FluentTextInput>().InvokeAsync(() => cut.FindComponent<FluentTextInput>().Instance.ValueChanged.InvokeAsync(groupName));
        Assert.Equal(groupName, Assert.Single(cut.FindAll(".resource-tag-group")).GetAttribute("aria-label"));
        await cut.FindComponent<FluentTextInput>().InvokeAsync(() => cut.FindComponent<FluentTextInput>().Instance.ValueChanged.InvokeAsync(string.Empty));

        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == "resource-grouping-None").OnClick!);
        Assert.Empty(cut.FindAll(".resource-tag-group"));
        Assert.Equal(["cache", "api", "worker"], cut.FindAll(".resource-row:not(.resource-row-all) .resource-row-name").Select(name => name.TextContent.Trim()));
    }

    [Theory]
    [InlineData("Tags")]
    [InlineData("ResourceType")]
    [InlineData("State")]
    public async Task AppHost_GroupingIsPersistedIndependentlyOfPaneNavigation(string grouping)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneGrouping ? (true, grouping) : (false, null);
        string? storedGrouping = null;
        storage.OnSetUnprotectedAsync = (key, value) =>
        {
            if (key == BrowserStorageKeys.ResourcePaneGrouping)
            {
                storedGrouping = Assert.IsType<string>(value);
            }
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.ResourceListUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".resource-tag-group")));
        navigation.NavigateTo(DashboardUrls.TelemetrySourcesUrl());
        cut.Render();
        Assert.Empty(cut.FindAll("#resourcePaneGroupingButton"));
        navigation.NavigateTo(DashboardUrls.ResourceListUrl());
        cut.Render();
        Assert.Single(cut.FindAll(".resource-tag-group"));

        cut.Find("#resourcePaneGroupingButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneGroupingButton");
        Assert.Equal($"resource-grouping-{grouping}", Assert.Single(menu.Instance.Items, item => item.Checked).Id);
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == "resource-grouping-None").OnClick!);
        Assert.Equal("None", storedGrouping);
        Assert.Empty(cut.FindAll(".resource-tag-group"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AppHost_LegacyTagsViewSelectsAndPersistsTagGrouping(bool fromStorage)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => fromStorage && key == BrowserStorageKeys.ResourcePaneMode ? (true, "Tags") : (false, null);
        string? storedGrouping = null;
        storage.OnSetUnprotectedAsync = (key, value) =>
        {
            if (key == BrowserStorageKeys.ResourcePaneGrouping)
            {
                storedGrouping = Assert.IsType<string>(value);
            }
        };
        Services.GetRequiredService<ResourceTagStore>().AddTag("api", "Backend");
        Services.GetRequiredService<NavigationManager>().NavigateTo(fromStorage ? DashboardUrls.ResourceListUrl() : DashboardUrls.TagsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Backend", Assert.Single(cut.FindAll(".resource-tag-group")).GetAttribute("aria-label"));
            Assert.Equal(ResourcePaneMode.Resources, Services.GetRequiredService<ResourcePaneState>().Mode);
            Assert.Equal("Tags", storedGrouping);
        });
    }

    [Fact]
    public async Task AppHost_StateGroupingCombinesUnknownStatesAndTracksResourceUpdates()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api"),
                ModelTestHelpers.CreateResource("worker", state: KnownResourceState.Unknown),
                ModelTestHelpers.CreateResource("hidden", state: KnownResourceState.Running, hidden: true),
                ModelTestHelpers.CreateResource("parameter", resourceType: KnownResourceTypes.Parameter)
            ], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>());
        storage.OnGetUnprotectedAsync = key => key == BrowserStorageKeys.ResourcePaneGrouping ? (true, "State") : (false, null);
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() =>
        {
            var group = Assert.Single(cut.FindAll(".resource-tag-group"));
            Assert.Equal("Unknown", group.GetAttribute("aria-label"));
            Assert.Equal(["api", "worker"], group.QuerySelectorAll(".resource-row-name").Select(name => name.TextContent.Trim()));
        });

        await channel.Writer.WriteAsync([new ResourceViewModelChange(ResourceViewModelChangeType.Upsert,
            ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running))]);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Unknown", "Running"], cut.FindAll(".resource-tag-group").Select(group => group.GetAttribute("aria-label")));
            Assert.Equal(["worker", "api"], cut.FindAll(".resource-tag-group .resource-row-name").Select(name => name.TextContent.Trim()));
        });
    }

    [Fact]
    public void AppHost_FilterMenuOnlyShowsTooltipOnButton()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.ResourceListUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        var button = cut.Find("#resourcePaneFilterButton");
        Assert.Equal("Filters", button.GetAttribute("data-tooltip"));
        Assert.Equal("Filters", button.GetAttribute("aria-label"));
        button.Click();

        var menu = cut.FindComponents<AspireMenu>().Single(menu => menu.Instance.Anchor == "resourcePaneFilterButton");
        var tooltipAnchors = menu.FindAll("[data-tooltip]");
        Assert.NotEmpty(tooltipAnchors);
        Assert.All(tooltipAnchors, anchor => Assert.Equal(string.Empty, anchor.GetAttribute("data-tooltip")));
        Assert.Empty(menu.FindComponents<FluentTooltip>());
    }

    [Theory]
    [InlineData("/structuredlogs")]
    [InlineData("/traces/detail/abc")]
    [InlineData("/metrics")]
    public async Task Standalone_RendersTelemetryResourcesAndTabs(string path)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var client = new TestDashboardClient(isEnabled: false, whenConnected: Task.FromCanceled(new CancellationToken(canceled: true)));
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, client);
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("worker");
        Services.GetRequiredService<ResourcePaneState>().SetMode(ResourcePaneMode.Tags);
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api", "worker"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["tab-StructuredLogs", "tab-Traces", "tab-Metrics"],
                cut.FindComponents<FluentTab>().Where(t => t.Instance.Visible).Select(t => t.Instance.Id));
            Assert.Equal(3, cut.FindAll(".resource-row").Count);
            Assert.Equal("2 sources", cut.Find(".resource-pane-foot").TextContent.Trim());
        });
        Assert.Equal(0, client.ResourceSubscriptionCount);
    }

    [Fact]
    public async Task Standalone_NewTelemetryAppearsAndSearchFiltersResources()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: false));
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        await AddTelemetryAsync("api");
        await AddTelemetryAsync("worker");
        cut.WaitForAssertion(() => Assert.Equal(["api", "worker"], cut.Instance.GetAllResourceNames()));

        await cut.FindComponent<FluentTextInput>().InvokeAsync(() =>
            cut.FindComponent<FluentTextInput>().Instance.ValueChanged.InvokeAsync("WORK"));

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll(".resource-row-link");
            Assert.Equal([DashboardUrls.StructuredLogsUrl(), DashboardUrls.StructuredLogsUrl("worker")],
                links.Select(link => link.GetAttribute("href")));
            Assert.Equal("1 source", cut.Find(".resource-pane-foot").TextContent.Trim());
            Assert.Equal(["api", "worker"], cut.Instance.GetAllResourceNames());
        });
    }

    [Fact]
    public async Task Standalone_SelectingResourcesAndTabsPreservesSelection()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: false));
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("worker");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.MetricsUrl("api"));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.Find(".resource-row-all a").Click();
        Assert.Equal("http://localhost/metrics", navigation.Uri);
        cut.Render();
        cut.FindAll(".resource-row-link").Single(link => link.GetAttribute("href") == DashboardUrls.MetricsUrl("api")).Click();
        cut.Render();
        cut.FindAll(".resource-row-link").Single(link => link.GetAttribute("href") == DashboardUrls.MetricsUrl("worker")).Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { CtrlKey = true });
        cut.Render();

        Assert.Equal(["api", "worker"], cut.Instance.SelectedResourceNames);
        Assert.Equal(2, cut.Instance.GetSelectionTelemetryKeys(cut.Instance.SelectedResourceNames)?.Count);
        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabChanged.InvokeAsync(
            cut.FindComponents<FluentTab>().Single(t => t.Instance.Id == "tab-Traces").Instance));
        Assert.Equal("http://localhost/traces?resource=api&resource=worker", navigation.Uri);
    }

    [Fact]
    public async Task Standalone_ChangingViewportPreservesTabsAndRoute()
    {
        var desktop = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var mobile = new ViewportInformation(IsDesktop: false, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesLayout(this, desktop, new TestDashboardClient(isEnabled: false));
        await AddTelemetryAsync("api");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.StructuredLogsUrl("api"));
        var cut = Render<CascadingValue<ViewportInformation>>(builder =>
            builder.Add(p => p.Value, desktop).AddChildContent<ResourcesLayout>());
        var tabs = cut.FindComponent<FluentTabs>().Instance;

        cut.Render(builder => builder.Add(p => p.Value, mobile).AddChildContent<ResourcesLayout>());
        Assert.Same(tabs, cut.FindComponent<FluentTabs>().Instance);
        Assert.Equal("http://localhost/structuredlogs/resource/api", navigation.Uri);
        cut.Find(".resource-pane-open-button").Click();
        Assert.True(cut.Find(".resources-layout").ClassList.Contains("drawer-open"));

        cut.Render(builder => builder.Add(p => p.Value, desktop).AddChildContent<ResourcesLayout>());
        Assert.Same(tabs, cut.FindComponent<FluentTabs>().Instance);
        Assert.Equal("http://localhost/structuredlogs/resource/api", navigation.Uri);
    }

    [Fact]
    public async Task AppHost_TelemetryPaneSeparatesExternalSourcesAndPreservesNavigationContext()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var client = new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, client);
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("browser");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsTelemetryPane);
            Assert.Equal(["api"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["api"], cut.Instance.GetSelectionTelemetryKeys(null)!.Select(key => key.Name));
            Assert.Equal([DashboardUrls.StructuredLogsUrl(), DashboardUrls.StructuredLogsUrl("api")],
                cut.FindAll(".resource-row-link").Select(link => link.GetAttribute("href")));
        });

        navigation.NavigateTo(DashboardUrls.TelemetrySourcesUrl());
        cut.Render();
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsTelemetryPane);
            Assert.Equal(["browser"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["browser"], cut.Instance.GetSelectionTelemetryKeys(null)!.Select(key => key.Name));
            Assert.Equal(["tab-StructuredLogs", "tab-Traces", "tab-Metrics"],
                cut.FindComponents<FluentTab>().Where(tab => tab.Instance.Visible).Select(tab => tab.Instance.Id));
            Assert.Equal(["/structuredlogs?pane=telemetry", "/structuredlogs/resource/browser?pane=telemetry"],
                cut.FindAll(".resource-row-link").Select(link => link.GetAttribute("href")));
        });
        cut.FindAll(".resource-row-link")[1].Click();
        cut.Render();
        Assert.Equal("/traces/resource/browser?pane=telemetry", cut.Instance.GetTabUrl(ResourcesLayout.ResourceTab.Traces));
        Assert.Equal("/metrics/resource/browser?duration=15&pane=telemetry",
            cut.Instance.AddSelectionToUrl("/metrics/resource/browser?duration=15"));
        Assert.Null(cut.Instance.GetSelectionTelemetryKeys(null));

        navigation.NavigateTo(DashboardUrls.TelemetrySourcesUrl());
        cut.Render();
        await AddTelemetryAsync("worker");
        cut.WaitForAssertion(() => Assert.Equal(["browser", "worker"],
            cut.Instance.GetSelectionTelemetryKeys(null)!.Select(key => key.Name)));

        navigation.NavigateTo(DashboardUrls.ResourceListUrl());
        cut.Render();
        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsTelemetryPane);
            Assert.Equal(["api"], cut.Instance.GetAllResourceNames());
        });
    }

    [Fact]
    public void AppHost_EmptyTelemetryPaneShowsNoSourcesAndNoTelemetryKeys()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.TelemetrySourcesUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("No telemetry sources", cut.Find(".resource-pane-message").TextContent.Trim());
            Assert.Empty(cut.Instance.GetAllResourceNames());
            Assert.Empty(cut.Instance.GetSelectionTelemetryKeys(null)!);
            Assert.Equal("All sources", cut.Find(".resource-row-all .resource-row-name").TextContent.Trim());
        });
    }

    [Fact]
    public async Task AppHost_DirectTelemetrySourceLinkSelectsTelemetryPane()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        await AddTelemetryAsync("browser");
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.TracesUrl("browser"));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsTelemetryPane);
            Assert.Equal(["browser"], cut.Instance.GetAllResourceNames());
            Assert.Equal("/traces/detail/abc?pane=telemetry", cut.Instance.AddPaneToUrl("/traces/detail/abc"));
        });
    }

    [Fact]
    public void AppHost_StillSubscribesToAppModelResources()
    {
        var viewport = new ViewportInformation(IsDesktop: false, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var client = new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, client);
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl("api"));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["tab-Overview", "tab-Console", "tab-StructuredLogs", "tab-Traces", "tab-Metrics"],
                cut.FindComponents<FluentTab>().Where(t => t.Instance.Visible).Select(t => t.Instance.Id));
        });
        Assert.Equal(1, client.ResourceSubscriptionCount);
        cut.Find(".resource-pane-open-button").Click();
        Assert.True(cut.Find(".resources-layout").ClassList.Contains("drawer-open"));
    }

    private Task AddTelemetryAsync(string name)
    {
        var resource = CreateResource(name);
        resource.Attributes.Remove(resource.Attributes.Single(attribute => attribute.Key == OtlpResource.SERVICE_INSTANCE_ID));
        return Services.GetRequiredService<SqliteTelemetryRepository>().AddLogsAsync(new AddContext(),
        [
            new ResourceLogs
            {
                Resource = resource,
                ScopeLogs = { new ScopeLogs { Scope = CreateScope(), LogRecords = { CreateLogRecord() } } }
            }
        ]);
    }
}
