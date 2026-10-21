// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Dashboard.Tests.TelemetryRepositoryTests;
using Aspire.Dashboard.Utils;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Components.Tests.Layout;

[UseCulture("en-US")]
public class ResourcesLayoutTests : DashboardTestContext
{
    [Theory]
    [InlineData(true, "/resources/api", "Resources")]
    [InlineData(false, "/resources/api", "Resources")]
    [InlineData(true, "/consolelogs/resource/api", "Console logs")]
    [InlineData(false, "/consolelogs/resource/api", "Console logs")]
    [InlineData(true, "/structuredlogs/resource/api", "Structured logs")]
    [InlineData(false, "/structuredlogs/resource/api", "Structured logs")]
    [InlineData(true, "/traces/resource/api", "Traces")]
    [InlineData(false, "/traces/resource/api", "Traces")]
    [InlineData(true, "/traces/detail/abc", "Traces")]
    [InlineData(false, "/traces/detail/abc", "Traces")]
    [InlineData(true, "/metrics/resource/api", "Metrics")]
    [InlineData(false, "/metrics/resource/api", "Metrics")]
    public void PageTitleAndResourceIdentityAreSeparate(bool isDesktop, string path, string expectedTitle)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running,
                    commands: [new CommandViewModel("restart", CommandViewModelState.Enabled, "Restart", "Restart the resource",
                        confirmationMessage: "", argumentInputs: [], isHighlighted: true, iconName: string.Empty, iconVariant: IconVariant.Regular)])
            ],
            resourceChannelProvider: () => channel));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(path.StartsWith("/traces/detail/", StringComparison.Ordinal) ? "/resources/api" : path);
        var header = Render<SectionOutlet>(builder => builder.Add(p => p.SectionName, MainLayout.HeaderContextSectionName));
        var cut = Render<ResourcesLayout>(builder => builder
            .AddCascadingValue(viewport)
            .Add(p => p.Body, content =>
            {
                content.OpenComponent<AspirePageContentLayout>(0);
                content.AddAttribute(1, nameof(AspirePageContentLayout.PageTitleSection),
                    (RenderFragment)(title => title.AddMarkupContent(0, "<h1 class=\"page-header\">Child page title</h1>")));
                content.CloseComponent();
            }));
        navigation.NavigateTo(path);
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(expectedTitle, (isDesktop ? header.Find("h1") : cut.Find("h1")).TextContent);
            var identity = cut.Find(".resource-tabs .resource-header-row");
            Assert.Equal("api", identity.QuerySelector("h2.resource-title")!.TextContent);
            Assert.Single(identity.QuerySelectorAll(".resource-header-icon svg"));
            Assert.Empty(cut.FindAll(".resource-tab-body h1"));
            Assert.Empty(header.FindAll(".resource-header-row"));
            Assert.Empty(cut.FindComponents<FluentTabs>());
            Assert.Equal(path == "/resources/api" ? ["Restart the resource", Resources.ControlsStrings.ActionsButtonText] : [],
                cut.FindAll(".resource-header-actions [data-tooltip]").Select(button => button.GetAttribute("data-tooltip")));
        });
    }

    [Theory]
    [InlineData(true, "/resources")]
    [InlineData(false, "/resources")]
    [InlineData(true, "/resources?view=graph")]
    [InlineData(false, "/resources?view=graph")]
    [InlineData(true, "/resources?resource=api&resource=worker")]
    [InlineData(false, "/resources?resource=api&resource=worker")]
    [InlineData(true, "/resources?view=graph&resource=api&resource=worker")]
    [InlineData(false, "/resources?view=graph&resource=api&resource=worker")]
    public void ResourceListKeepsResourcesPageTitleAndViewTabs(bool isDesktop, string path)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        Assert.IsType<TestSessionStorage>(Services.GetRequiredService<ISessionStorage>()).OnGetAsync =
            key => key == BrowserStorageKeys.LastSelectedResources ? (true, new List<string>()) : (false, null);
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var header = Render<SectionOutlet>(builder => builder.Add(p => p.SectionName, MainLayout.HeaderContextSectionName));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal("Resources", (isDesktop ? header.Find("h1") : cut.Find("h1")).TextContent);
            Assert.Equal(["Table", "Graph"], cut.FindComponents<FluentTab>().Select(tab => tab.Find(".tab-label").TextContent.Trim()));
            Assert.Equal(isDesktop ? ["resource-tabs-nav", "resource-tab-toolbar"] : ["resource-tabs-nav"],
                cut.Find(".resource-tabs").Children.Select(element => element.ClassName));
            Assert.Equal(ResourcesLayout.ParseLocation(path).SelectedResourceNames, cut.Instance.SelectedResourceNames);
            Assert.Empty(header.FindAll(".resource-header-row"));
        });
    }

    [Theory]
    [InlineData(true, "/consolelogs")]
    [InlineData(false, "/consolelogs")]
    [InlineData(true, "/structuredlogs")]
    [InlineData(false, "/structuredlogs")]
    [InlineData(true, "/traces")]
    [InlineData(false, "/traces")]
    [InlineData(true, "/traces/detail/abc")]
    [InlineData(false, "/traces/detail/abc")]
    [InlineData(true, "/metrics")]
    [InlineData(false, "/metrics")]
    public void MultiSelectionDisplaysResourceNamesInTelemetryHeader(bool isDesktop, string path)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        var resourcePath = path.StartsWith("/traces/detail/", StringComparison.Ordinal) ? "/traces" : path;
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"{resourcePath}?resource=api&resource=worker");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        if (path != resourcePath)
        {
            navigation.NavigateTo(path);
            cut.Render();
        }

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api", "worker"], cut.Instance.SelectedResourceNames);
            Assert.Equal(["api", "worker"],
                cut.FindAll(".resource-row.selected").Select(element => element.GetAttribute("data-resource-name")));
            Assert.Equal(isDesktop ? ["resource-header-row", "resource-tab-toolbar"] : ["resource-header-row"],
                cut.Find(".resource-tabs").Children.Select(element => element.ClassName));
            Assert.Equal("2 selected resources", cut.Find(".resource-tabs h2.resource-title").TextContent);
            Assert.Equal(["api", "worker"], cut.FindAll(".resource-header-chip-name").Select(element => element.TextContent));
            Assert.Equal([$"{resourcePath}/resource/api", $"{resourcePath}/resource/worker"],
                cut.FindAll(".resource-header-chip").Select(element => element.GetAttribute("href")));
            Assert.Empty(cut.FindAll(".resource-header-actions"));
        });

        navigation.NavigateTo(cut.FindAll(".resource-header-chip")[1].GetAttribute("href")!);
        cut.Render();
        Assert.Equal($"http://localhost{resourcePath}/resource/worker", navigation.Uri);
        Assert.Equal(["worker"], cut.Instance.SelectedResourceNames);
        Assert.Equal("worker", cut.Find(".resource-tabs h2.resource-title").TextContent);
        Assert.Empty(cut.FindAll(".resource-header-chip"));
    }

    [Theory]
    [InlineData(true, "/consolelogs")]
    [InlineData(false, "/consolelogs")]
    [InlineData(true, "/structuredlogs")]
    [InlineData(false, "/structuredlogs")]
    [InlineData(true, "/traces")]
    [InlineData(false, "/traces")]
    [InlineData(true, "/metrics")]
    [InlineData(false, "/metrics")]
    public async Task ResourceSelector_ResourcesWithoutTelemetryKeepCurrentPage(bool isDesktop, string path)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("cache")],
            resourceChannelProvider: () => channel));
        await AddTelemetryAsync("api");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"{path}/resource/api");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal($"{path}/resource/cache",
                cut.Find("[data-resource-name='cache'] .resource-row-link").GetAttribute("href"));
        });

        cut.Find("[data-resource-name='cache'] .resource-row-link").Click();
        cut.Render();
        Assert.Equal($"http://localhost{path}/resource/cache", navigation.Uri);
        Assert.Equal(["cache"], cut.Instance.SelectedResourceNames);
        Assert.Equal(ResourcesLayout.ParseLocation(path).Tab, cut.Instance.CurrentTab);

        cut.Find("[data-resource-name='api'] .resource-row-link")
            .Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { CtrlKey = true });
        cut.Render();
        var multiSelectionPath = path == "/metrics" ? $"{path}/resource/cache" : path;
        Assert.Equal($"http://localhost{multiSelectionPath}?resource=cache&resource=api", navigation.Uri);
        Assert.Equal(["cache", "api"], cut.Instance.SelectedResourceNames);

        cut.Find("[data-resource-name='api'] .resource-row-link")
            .Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { CtrlKey = true });
        cut.Render();
        Assert.Equal($"http://localhost{path}/resource/cache", navigation.Uri);
        Assert.Equal(["cache"], cut.Instance.SelectedResourceNames);

        cut.Find(".resource-row-all a").Click();
        cut.Render();
        Assert.Equal($"http://localhost{path}", navigation.Uri);
        Assert.Empty(cut.Instance.SelectedResourceNames);
    }

    [Theory]
    [InlineData("/structuredlogs")]
    [InlineData("/traces")]
    [InlineData("/metrics")]
    public async Task TelemetrySelectionUsesCurrentLocationBeforeLayoutRerenders(string path)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("cache")],
            resourceChannelProvider: () => channel));
        await AddTelemetryAsync("api");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"{path}/resource/api");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        Assert.Equal(["api"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));

        navigation.NavigateTo($"{path}/resource/cache");
        Assert.Equal(["api"], cut.Instance.SelectedResourceNames);
        Assert.Empty(cut.Instance.GetSelectionTelemetryKeys(null)!);

        navigation.NavigateTo($"{path}?resource=cache&resource=missing");
        Assert.Empty(cut.Instance.GetSelectionTelemetryKeys(null)!);

        navigation.NavigateTo(path);
        Assert.Equal(["api"], cut.Instance.GetSelectionTelemetryKeys(null)!.Select(key => key.Name));
    }

    [Fact]
    public async Task SingleSelection_UninstrumentedPeerDoesNotIncludeOtherResourcesLogs()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var cache = ModelTestHelpers.CreateResource("cache");
        Services.AddSingleton<IOutgoingPeerResolver>(new TestOutgoingPeerResolver(_ => (cache.Name, cache)));
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [cache],
            resourceChannelProvider: () => channel));
        await AddTelemetryAsync("api");
        var repository = Services.GetRequiredService<SqliteTelemetryRepository>();
        var time = DateTime.UtcNow;
        await repository.AddTracesAsync(new AddContext(),
        [
            new ResourceSpans
            {
                Resource = CreateResource("api"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "trace-1", spanId: "span-1", startTime: time.AddSeconds(-1), endTime: time,
                                kind: Span.Types.SpanKind.Client,
                                attributes: [KeyValuePair.Create(OtlpSpan.PeerServiceAttributeKey, "cache")])
                        }
                    }
                }
            }
        ]);
        var peer = repository.GetResourceByCompositeName("cache");
        Assert.NotNull(peer);
        Assert.True(peer.UninstrumentedPeer);

        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl("cache"));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        var keys = cut.Instance.GetSelectionTelemetryKeys(null);
        Assert.Equal([peer.ResourceKey], keys);

        var viewModel = Services.GetRequiredService<StructuredLogsViewModel>();
        viewModel.ResourceKeys = keys;
        viewModel.Count = 10;
        var logs = await viewModel.GetLogsAsync(CancellationToken.None);
        Assert.Equal(0, logs.TotalItemCount);
        Assert.Empty(logs.Items);
    }

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

        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
        var otherMode = otherModes.Keys.First();
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{otherMode}").OnClick!);
        Assert.False(await cut.InvokeAsync(() => cut.Instance.ReorderGroupAsync(grouping, movedKey, targetKey, after: false)));
        cut.Find("#resourcePaneFilterButton").Click();
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
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        Assert.Equal(
            [
                ("All sources", "1 source", (string?)null),
                ("api", $"N/A; Telemetry; {Resources.Layout.ResourcePaneTelemetryOnly}", null)
            ],
            cut.FindAll(".resource-row-link").Select(link => (
                link.GetAttribute("aria-label"), link.GetAttribute("aria-description"), link.GetAttribute("data-tooltip"))));
    }

    [Fact]
    public async Task AppHost_UngroupedRowsMatchTableOrderAndPreserveHierarchy()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("worker", resourceType: "Project"),
                ModelTestHelpers.CreateResource("root"),
                ModelTestHelpers.CreateResource("z-child", resourceType: "ZType", properties: new()
                {
                    [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
                }),
                ModelTestHelpers.CreateResource("a-child", resourceType: "Executable", properties: new()
                {
                    [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
                }),
                ModelTestHelpers.CreateResource("cache"),
                ModelTestHelpers.CreateResource("api-zzz", displayName: "api", resourceType: "Project"),
                ModelTestHelpers.CreateResource("api-admin-aaa", displayName: "api-admin", resourceType: "Project"),
                ModelTestHelpers.CreateResource("api-aaa", displayName: "api", resourceType: "Project")
            ], resourceChannelProvider: () => channel));
        var store = Services.GetRequiredService<ResourceOrderStore>();
        store.EnsureResources(["worker_0", "root_0", "z-child_0", "a-child_0", "cache_0", "api_0", "api-admin_0"]);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(["cache", "root", "a-child", "z-child", "api-aaa", "api-zzz", "api-admin-aaa", "worker"], GetNames()));
        Assert.Empty(cut.FindAll(".resource-row-drag-handle"));

        cut.Find("[data-resource-name='root'] .resource-row-toggle").Click();
        Assert.Equal(["cache", "root", "api-aaa", "api-zzz", "api-admin-aaa", "worker"], GetNames());
        cut.Find("[data-resource-name='root'] .resource-row-toggle").Click();

        await channel.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("0", resourceType: "Executable", properties: new()
        {
            [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("root"), false, null, 0, null, false)
        }))]);
        cut.WaitForAssertion(() => Assert.Equal(["cache", "root", "0", "a-child", "z-child", "api-aaa", "api-zzz", "api-admin-aaa", "worker"], GetNames()));

        string?[] GetNames() => cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name")).ToArray();
    }

    [Fact]
    public async Task AppHost_NewResourcesAndRunSpecificNamesStaySorted()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var originalApi = ModelTestHelpers.CreateResource("api-first-run", displayName: "api");
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [originalApi, ModelTestHelpers.CreateResource("cache")], resourceChannelProvider: () => channel));
        Services.GetRequiredService<ResourceOrderStore>().EnsureResources(["cache_0", "api_0"]);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(["api-first-run", "cache"],
            cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name"))));

        await channel.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("aaa"))]);
        cut.WaitForAssertion(() => Assert.Equal(["aaa", "api-first-run", "cache"],
            cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name"))));
        await channel.Writer.WriteAsync([
            new(ResourceViewModelChangeType.Delete, originalApi),
            new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("api-second-run", displayName: "api"))
        ]);
        cut.WaitForAssertion(() => Assert.Equal(["aaa", "api-second-run", "cache"],
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
    public async Task AppHost_UngroupedSortingIsRestoredAfterSearchFiltersAndGrouping(string mode)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.Exited)
            ], resourceChannelProvider: () => channel));
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.Equal(["cache", "api"],
            cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name"))));
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
            cut.Find("#resourcePaneFilterButton").Click();
            var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
            await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{mode}").OnClick!);
            reset = menu.Instance.Items.Single(item => item.Id == "resource-grouping-None").OnClick!;
        }

        Assert.Empty(cut.FindAll(".resource-row-drag-handle"));
        await cut.InvokeAsync(reset);
        Assert.Equal(["cache", "api"], cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name")));
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

        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
        Assert.Equal(["None", "Tags", "Resource type", "State"],
            menu.Instance.Items.Where(item => item.Role == MenuItemRole.Radio).Select(item => item.Text));
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
    public async Task AppHost_GroupingIsPersistedAcrossResourceAndTelemetryPages(string grouping)
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
        navigation.NavigateTo(DashboardUrls.StructuredLogsUrl());
        cut.Render();
        Assert.Single(cut.FindAll("#resourcePaneFilterButton"));
        Assert.Single(cut.FindAll(".resource-tag-group"));
        navigation.NavigateTo(DashboardUrls.ResourceListUrl());
        cut.Render();
        Assert.Single(cut.FindAll(".resource-tag-group"));

        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
        Assert.Equal($"resource-grouping-{grouping}", Assert.Single(menu.Instance.Items, item => item.Role == MenuItemRole.Radio && item.Checked).Id);
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
        Assert.True(Assert.Single(menu.Instance.Items, item => item.Text == Resources.ControlsStrings.ShowHiddenResources).IsDisabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResourceSelector_FilterMenuContainsGroupingAndHiddenResources(bool isDesktop)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running),
                ModelTestHelpers.CreateResource("hidden", state: KnownResourceState.Running, hidden: true)
            ], resourceChannelProvider: () => channel));
        var storage = Assert.IsType<TestSessionStorage>(Services.GetRequiredService<ISessionStorage>());
        var storedVisibility = new List<bool>();
        storage.OnSetAsync = (key, value) =>
        {
            if (key == BrowserStorageKeys.ResourcesShowHiddenResources)
            {
                storedVisibility.Add(Assert.IsType<bool>(value));
            }
        };
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.ResourceListUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["All resources", "api"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
        });
        Assert.Equal(["resourcePaneFilterButton"], cut.FindComponents<AspireMenuButton>()
            .Where(button => button.Instance.ButtonClass == "resource-pane-button")
            .Select(button => button.Instance.MenuButtonId));
        Assert.Empty(cut.FindAll(".resource-pane-foot button"));

        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
        Assert.Equal(["Group resources", "None", "Tags", "Resource type", "State"],
            menu.Instance.Items.Take(5).Select(item => item.Text));
        Assert.True(menu.Instance.Items[0].IsGroupHeader);
        Assert.All(menu.Instance.Items.Skip(1).Take(4), item => Assert.Equal(MenuItemRole.Radio, item.Role));
        Assert.True(menu.Instance.Items[5].IsDivider);
        Assert.Equal(["resource-grouping-header", "resource-filter-types-header", "resource-filter-states-header", "resource-filter-health-header"],
            menu.Instance.Items.Where(item => item.IsGroupHeader).Select(item => item.Id));

        var showHidden = menu.Instance.Items[^1];
        Assert.Equal(Resources.ControlsStrings.ShowHiddenResources, showHidden.Text);
        Assert.False(showHidden.IsDisabled);
        Assert.NotNull(showHidden.OnClick);
        await cut.InvokeAsync(showHidden.OnClick);
        cut.WaitForAssertion(() => Assert.Equal(["All resources", "api", "hidden"],
            cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim())));
        Assert.Equal([true], storedVisibility);

        var hideHidden = menu.Instance.Items[^1];
        Assert.Equal(Resources.ControlsStrings.HideHiddenResources, hideHidden.Text);
        Assert.NotNull(hideHidden.OnClick);
        await cut.InvokeAsync(hideHidden.OnClick);
        cut.WaitForAssertion(() => Assert.Equal(["All resources", "api"],
            cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim())));
        Assert.Equal([true, false], storedVisibility);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResourceSelector_StatusFiltersOnlyAppearInFilterMenu(bool isDesktop)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running),
                ModelTestHelpers.CreateResource("worker", state: KnownResourceState.NotStarted)
            ], resourceChannelProvider: () => channel));
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        var expectedSections = new List<string> { "resource-pane-head", "resource-pane-list", "resource-pane-foot" };
        if (isDesktop)
        {
            expectedSections.Add("resource-pane-resizer");
        }

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(expectedSections, cut.Find(".resource-pane").Children.Select(element => element.ClassName));
            Assert.Equal(["All resources", "api", "worker"],
                cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
        });

        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenu>().Single(menu => menu.Instance.Anchor == "resourcePaneFilterButton");
        var stateFilters = menu.Instance.Items.Where(item => item.Role == MenuItemRole.Checkbox &&
            item.Id?.StartsWith("resource-filter-states-", StringComparison.Ordinal) == true).ToList();
        Assert.Equal([Resources.ControlsStrings.LabelAll, nameof(KnownResourceState.NotStarted), nameof(KnownResourceState.Running)], stateFilters.Select(item => item.Text));

        var runningFilter = stateFilters.Single(item => item.Text == nameof(KnownResourceState.Running));
        Assert.NotNull(runningFilter.OnCheckedChanged);
        await cut.InvokeAsync(() => runningFilter.OnCheckedChanged(false));
        cut.WaitForAssertion(() => Assert.Equal(["All resources", "worker"],
            cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim())));
    }

    [Theory]
    [InlineData("/structuredlogs")]
    [InlineData("/traces/detail/abc")]
    [InlineData("/metrics")]
    public async Task Standalone_RendersTelemetryResourcesWithoutTabs(string path)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var client = new TestDashboardClient(isEnabled: false, whenConnected: Task.FromCanceled(new CancellationToken(canceled: true)));
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, client);
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("worker");
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api", "worker"], cut.Instance.GetAllResourceNames());
            Assert.Empty(cut.FindComponents<FluentTabs>());
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
    public async Task Standalone_SelectingResourcesAndMainNavigationPreservesSelection()
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
        Assert.Equal("2 sources selected", cut.Find(".resource-tabs h2.resource-title").TextContent);
        Assert.Equal(["api", "worker"], cut.FindAll(".resource-header-chip-name").Select(element => element.TextContent));
        Render<FluentTooltipProvider>();
        var menu = Render<DesktopNavMenu>();
        navigation.NavigateTo(menu.Find("#main-rail-traces").GetAttribute("href")!);
        Assert.Equal("http://localhost/traces?resource=api&resource=worker", navigation.Uri);
    }

    [Fact]
    public async Task Standalone_ChangingViewportPreservesRouteWithoutTabs()
    {
        var desktop = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var mobile = new ViewportInformation(IsDesktop: false, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesLayout(this, desktop, new TestDashboardClient(isEnabled: false));
        await AddTelemetryAsync("api");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.StructuredLogsUrl("api"));
        var cut = Render<CascadingValue<ViewportInformation>>(builder =>
            builder.Add(p => p.Value, desktop).AddChildContent<ResourcesLayout>());
        Assert.Empty(cut.FindComponents<FluentTabs>());

        cut.Render(builder => builder.Add(p => p.Value, mobile).AddChildContent<ResourcesLayout>());
        Assert.Empty(cut.FindComponents<FluentTabs>());
        Assert.Equal("http://localhost/structuredlogs/resource/api", navigation.Uri);
        cut.Find(".resource-pane-open-button").Click();
        Assert.True(cut.Find(".resources-layout").ClassList.Contains("drawer-open"));

        cut.Render(builder => builder.Add(p => p.Value, desktop).AddChildContent<ResourcesLayout>());
        Assert.Empty(cut.FindComponents<FluentTabs>());
        Assert.Equal("http://localhost/structuredlogs/resource/api", navigation.Uri);
    }

    [Theory]
    [InlineData("/structuredlogs", true)]
    [InlineData("/structuredlogs", false)]
    [InlineData("/traces", true)]
    [InlineData("/traces", false)]
    [InlineData("/metrics", true)]
    [InlineData("/metrics", false)]
    public async Task AppHost_TelemetryPagesCombineResourcesAndExternalSources(string path, bool isDesktop)
    {
        var viewport = new ViewportInformation(IsDesktop: isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: !isDesktop);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var client = new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel);
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, client);
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("browser");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api", "browser"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["api", "browser"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));
            Assert.Equal([path, $"{path}/resource/api", $"{path}/resource/browser"],
                cut.FindAll(".resource-row-link").Select(link => link.GetAttribute("href")));
            Assert.Equal("2 resources", cut.Find(".resource-pane-foot").TextContent.Trim());
        });

        cut.FindAll(".resource-row-link")[2].Click();
        cut.Render();
        Assert.Equal("/traces/resource/browser", cut.Instance.GetTabUrl(ResourcesLayout.ResourceTab.Traces));
        Assert.Equal("/metrics/resource/browser?duration=15",
            cut.Instance.AddSelectionToUrl("/metrics/resource/browser?duration=15"));
        Assert.Equal(["browser"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));

        cut.FindAll(".resource-row-link")[1].Click();
        cut.Render();
        cut.FindAll(".resource-row-link")[2].Click(new MouseEventArgs { CtrlKey = true });
        cut.Render();
        Assert.Equal(["api", "browser"], cut.Instance.SelectedResourceNames);
        Assert.Equal(["api", "browser"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));
        Assert.Equal("/traces?resource=api&resource=browser", cut.Instance.GetTabUrl(ResourcesLayout.ResourceTab.Traces));
        Assert.Equal(["api", "browser"], cut.FindAll(".resource-header-chip-name").Select(element => element.TextContent.Trim()));

        navigation.NavigateTo(path);
        cut.Render();
        await AddTelemetryAsync("worker");
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["api", "browser", "worker"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["api", "browser", "worker"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));
        });
    }

    [Theory]
    [InlineData("/structuredlogs", true, false)]
    [InlineData("/structuredlogs", false, false)]
    [InlineData("/structuredlogs", true, true)]
    [InlineData("/traces", true, false)]
    [InlineData("/traces", false, false)]
    [InlineData("/traces", true, true)]
    [InlineData("/metrics", true, false)]
    [InlineData("/metrics", false, false)]
    [InlineData("/metrics", true, true)]
    public async Task TelemetryOnlyResourcesHaveNormalMetadataAndTableOrdering(string path, bool isDesktop, bool collapsed)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: !isDesktop);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("server", resourceType: "ZType"),
                ModelTestHelpers.CreateResource("api", resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.NotStarted),
                ModelTestHelpers.CreateResource("nested", resourceType: "ZType", properties: new()
                {
                    [KnownProperties.Resource.ParentName] = new(KnownProperties.Resource.ParentName, Value.ForString("api"), false, null, 0, null, false)
                })
            ], resourceChannelProvider: () => channel));
        Assert.IsType<TestLocalStorage>(Services.GetRequiredService<ILocalStorage>()).OnGetUnprotectedAsync =
            key => key == BrowserStorageKeys.ResourcePaneCollapsed ? (true, collapsed) : (false, null);
        await AddTelemetryAsync("browser-z");
        await AddTelemetryAsync("browser-a");
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() => Assert.Equal(["cache", "api", "nested", "browser-a", "browser-z", "server"], GetNames()));
        Assert.Equal(["api", "cache", "nested", "server"], cut.Instance.ResourceByName.Keys.Order());
        foreach (var name in new[] { "browser-a", "browser-z" })
        {
            var row = cut.Find($"[data-resource-name='{name}']");
            var link = row.QuerySelector(".resource-row-link")!;
            Assert.Equal($"{path}/resource/{name}", link.GetAttribute("href"));
            Assert.Equal($"N/A; Telemetry; {Resources.Layout.ResourcePaneTelemetryOnly}", link.GetAttribute("aria-description"));
            Assert.Equal(cut.Find("[data-resource-name='cache'] .state-dot").ClassName, row.QuerySelector(".state-dot")!.ClassName);
            if (collapsed)
            {
                Assert.Empty(row.QuerySelectorAll(".resource-row-text"));
            }
            else
            {
                Assert.Equal("N/A", row.QuerySelector(".resource-row-sub > span:nth-child(2)")!.TextContent.Trim());
                Assert.Equal("Telemetry", row.QuerySelector(".resource-row-type")!.TextContent);
                Assert.Equal("6 resources", cut.Find(".resource-pane-foot > span").TextContent.Trim());
            }
        }

        cut.Find("[data-resource-name='api'] .resource-row-link").Click();
        cut.Render();
        cut.Find("[data-resource-name='browser-z'] .resource-row-link").Click(new MouseEventArgs { ShiftKey = true });
        cut.Render();
        Assert.Equal(["api", "nested", "browser-a", "browser-z"], cut.Instance.SelectedResourceNames);
        Assert.Equal(["browser-a", "browser-z"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));

        await AddTelemetryAsync("browser-b");
        cut.WaitForAssertion(() => Assert.Equal(["cache", "api", "nested", "browser-a", "browser-b", "browser-z", "server"], GetNames()));

        string?[] GetNames() => cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name")).ToArray();
    }

    [Theory]
    [InlineData("Tags", true)]
    [InlineData("Tags", false)]
    [InlineData("ResourceType", true)]
    [InlineData("ResourceType", false)]
    [InlineData("State", true)]
    [InlineData("State", false)]
    public async Task TelemetryOnlyResourcesParticipateInGroupingAndFiltering(string grouping, bool isDesktop)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: !isDesktop);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("cache", state: KnownResourceState.NotStarted),
                ModelTestHelpers.CreateResource("server", state: KnownResourceState.Running, resourceType: "ZType")
            ], resourceChannelProvider: () => channel));
        var tags = Services.GetRequiredService<ResourceTagStore>();
        tags.AddTag("api", "Backend");
        tags.AddTag("server", "Backend");
        tags.AddTag("browser", "Backend");
        await AddTelemetryAsync("browser");
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        cut.Find("#resourcePaneFilterButton").Click();
        var menu = cut.FindComponents<AspireMenuButton>().Single(button => button.Instance.MenuButtonId == "resourcePaneFilterButton");
        await cut.InvokeAsync(menu.Instance.Items.Single(item => item.Id == $"resource-grouping-{grouping}").OnClick!);

        var expectedGroups = grouping switch
        {
            "Tags" => new[] { "Backend", "Untagged" },
            "ResourceType" => ["Container", "Project", "Telemetry", "ZType"],
            _ => ["N/A", "Not started", "Running"]
        };
        var telemetryGroupName = grouping switch { "Tags" => "Untagged", "ResourceType" => "Telemetry", _ => "N/A" };
        var expectedGroupResources = grouping == "Tags" ? new[] { "cache", "browser" } : ["browser"];
        Assert.Equal(expectedGroups, cut.FindAll(".resource-tag-group").Select(group => group.GetAttribute("aria-label")));
        Assert.Equal(expectedGroupResources, GetTelemetryGroupNames());
        Assert.Equal("4 resources", cut.Find(".resource-pane-foot > span").TextContent.Trim());

        cut.Find($".resource-tag-group[aria-label='{telemetryGroupName}'] .resource-tag-link").Click();
        cut.Render();
        Assert.Equal(expectedGroupResources, cut.Instance.SelectedResourceNames);
        Assert.Equal(["browser"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));

        var input = cut.FindComponent<FluentTextInput>();
        await cut.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync("browser"));
        Assert.Equal(telemetryGroupName, Assert.Single(cut.FindAll(".resource-tag-group")).GetAttribute("aria-label"));
        Assert.Equal(["browser"], GetTelemetryGroupNames());
        await cut.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(string.Empty));

        var modelOnlyNames = grouping == "Tags" ? new[] { "api", "server", "cache" } : ["cache", "api", "server"];
        foreach (var filterKey in new[] { "types:Telemetry", "states:N/A", "health:" })
        {
            var filter = menu.Instance.Items.Single(item => item.RenderKey is string key && key == filterKey);
            await cut.InvokeAsync(() => filter.OnCheckedChanged!(false));
            Assert.Equal(filterKey == "health:" ? ["api", "server"] : modelOnlyNames,
                cut.FindAll("[data-resource-name]").Select(row => row.GetAttribute("data-resource-name")));
            await cut.InvokeAsync(() => filter.OnCheckedChanged!(true));
            Assert.Equal(expectedGroupResources, GetTelemetryGroupNames());
        }

        await AddTelemetryAsync("aaa-browser");
        cut.WaitForAssertion(() => Assert.Equal(grouping == "Tags" ? ["cache", "aaa-browser", "browser"] : new[] { "aaa-browser", "browser" },
            GetTelemetryGroupNames()));

        string[] GetTelemetryGroupNames() => cut.Find($".resource-tag-group[aria-label='{telemetryGroupName}']")
            .QuerySelectorAll(".resource-row-name").Select(element => element.TextContent.Trim()).ToArray();
    }

    [Fact]
    public void AppHost_UninstrumentedResourcesRemainInTelemetrySelector()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        Services.GetRequiredService<NavigationManager>().NavigateTo(DashboardUrls.StructuredLogsUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Empty(cut.FindAll(".resource-pane-message"));
            Assert.Equal(["api"], cut.Instance.GetAllResourceNames());
            Assert.Empty(cut.Instance.GetSelectionTelemetryKeys(null));
            Assert.Equal(["All resources", "api"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
        });
    }

    [Theory]
    [InlineData("/structuredlogs/resource/browser")]
    [InlineData("/traces/resource/browser")]
    [InlineData("/metrics/resource/browser")]
    [InlineData("/traces/resource/browser?pane=telemetry")]
    public async Task AppHost_DirectTelemetrySourceLinkSelectsSourceInCombinedSelector(string path)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        await AddTelemetryAsync("browser");
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api", "browser"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["browser"], cut.Instance.SelectedResourceNames);
            Assert.Equal(["browser"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));
            Assert.Equal("browser", cut.Find(".resource-row.selected .resource-row-name").TextContent.Trim());
            Assert.Equal("/traces/resource/browser", cut.Instance.GetTabUrl(ResourcesLayout.ResourceTab.Traces));
        });
    }

    [Theory]
    [InlineData("/resources?resource=", true)]
    [InlineData("/resources?resource=", false)]
    [InlineData("/consolelogs", true)]
    [InlineData("/consolelogs", false)]
    public async Task AppHost_ResourceAndConsolePagesOnlyListAppModelResources(string path, bool isDesktop)
    {
        var viewport = new ViewportInformation(IsDesktop: isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: !isDesktop);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        await AddTelemetryAsync("api");
        await AddTelemetryAsync("browser");
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["api"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["api"], cut.Instance.GetSelectionTelemetryKeys(null).Select(key => key.Name));
            Assert.Equal(["All resources", "api"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AppHost_NavigationDropsTelemetryOnlySelectionWithoutRestoringPreviousResource(bool consoleLogs, bool mixedSelection)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        Assert.IsType<TestSessionStorage>(Services.GetRequiredService<ISessionStorage>()).OnGetAsync =
            key => key == BrowserStorageKeys.LastSelectedResources ? (true, new List<string> { "worker" }) : (false, null);
        await AddTelemetryAsync("browser");
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(mixedSelection ? "/traces?resource=api&resource=browser" : "/traces/resource/browser");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));

        navigation.NavigateTo("/traces/detail/abc");
        cut.Render();
        var destination = consoleLogs ? DesktopNavMenu.NavSection.ConsoleLogs : DesktopNavMenu.NavSection.Resources;
        var item = DesktopNavMenu.GetItems(true, false, false, cut.Instance.Loc,
            navigation.ToBaseRelativePath(navigation.Uri), Services.GetRequiredService<ResourcePaneState>())
            .Single(item => item.Section == destination);
        navigation.NavigateTo(item.Href);
        cut.Render();

        Assert.Equal(mixedSelection ? ["api"] : [], cut.Instance.SelectedResourceNames);
        Assert.Equal(["All resources", "api", "worker"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
        if (!mixedSelection)
        {
            Assert.Equal("All resources", cut.Find(".resource-row.selected .resource-row-name").TextContent.Trim());
        }
    }

    [Fact]
    public void AppHost_ExplicitAllResourcesUrlDoesNotRestorePreviousSelectionOnFirstLoad()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        Assert.IsType<TestSessionStorage>(Services.GetRequiredService<ISessionStorage>()).OnGetAsync =
            key => key == BrowserStorageKeys.LastSelectedResources ? (true, new List<string> { "api" }) : (false, null);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.ResourceListUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Empty(cut.Instance.SelectedResourceNames);
            Assert.Equal("All resources", cut.Find(".resource-row.selected .resource-row-name").TextContent.Trim());
            Assert.Equal(navigation.ToAbsoluteUri(DashboardUrls.ResourceListUrl()).AbsoluteUri, navigation.Uri);
        });
    }

    [Fact]
    public async Task AppHost_TelemetrySourceBecomingAppModelResourceUpdatesSelectorAndNavigation()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api")], resourceChannelProvider: () => channel));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(DashboardUrls.TracesUrl());
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsLoaded));
        await AddTelemetryAsync("browser");
        var paneState = Services.GetRequiredService<ResourcePaneState>();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["api", "browser"], cut.Instance.GetAllResourceNames());
            Assert.Equal(["api"], paneState.ExcludeTelemetryOnlyResources(["api", "browser"]));
            Assert.Equal("Telemetry", cut.Find("[data-resource-name='browser'] .resource-row-type").TextContent);
        });
        var browser = ModelTestHelpers.CreateResource("browser");
        await channel.Writer.WriteAsync([new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, browser)]);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["All resources", "api", "browser"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
            Assert.Equal(["api", "browser"], paneState.ExcludeTelemetryOnlyResources(["api", "browser"]));
            Assert.Equal("Container", cut.Find("[data-resource-name='browser'] .resource-row-type").TextContent);
        });

        await channel.Writer.WriteAsync([new ResourceViewModelChange(ResourceViewModelChangeType.Delete, browser)]);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["All resources", "api", "browser"], cut.FindAll(".resource-row-name").Select(element => element.TextContent.Trim()));
            Assert.Equal(["api"], paneState.ExcludeTelemetryOnlyResources(["api", "browser"]));
            Assert.Equal("Telemetry", cut.Find("[data-resource-name='browser'] .resource-row-type").TextContent);
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
            Assert.Empty(cut.FindComponents<FluentTabs>());
        });
        Assert.Equal(1, client.ResourceSubscriptionCount);
        cut.Find(".resource-pane-open-button").Click();
        Assert.True(cut.Find(".resources-layout").ClassList.Contains("drawer-open"));
    }

    [Theory]
    [InlineData("/resources", true, true)]
    [InlineData("/resources?resource=api&resource=worker", true, true)]
    [InlineData("/resources?view=graph&resource=api&resource=worker", true, true)]
    [InlineData("/resources/api", true, false)]
    [InlineData("/structuredlogs?resource=api&resource=worker", true, false)]
    [InlineData("/resources", false, true)]
    [InlineData("/resources?resource=api&resource=worker", false, true)]
    [InlineData("/resources/api", false, false)]
    [InlineData("/metrics", false, false)]
    public void ResourceViewTabs_OnlyShownForAllOrMultipleResources(string path, bool isDesktop, bool showTabs)
    {
        var viewport = new ViewportInformation(isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        Assert.IsType<TestSessionStorage>(Services.GetRequiredService<ISessionStorage>()).OnGetAsync =
            key => key == BrowserStorageKeys.LastSelectedResources ? (true, new List<string>()) : (false, null);
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(showTabs, cut.Instance.ShowResourceViewTabs);
            Assert.Equal(showTabs ? ["tab-Overview", "tab-Graph"] : [],
                cut.FindComponents<FluentTab>().Select(tab => tab.Instance.Id));
        });
    }

    [Fact]
    public async Task ResourceViewTabs_SwitchViewsAndPreserveSelection()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/resources?resource=api&resource=worker");
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabChanged.InvokeAsync(
            cut.FindComponents<FluentTab>().Single(tab => tab.Instance.Id == "tab-Graph").Instance));
        Assert.Equal("http://localhost/resources?view=graph&resource=api&resource=worker", navigation.Uri);
        cut.Render();
        Assert.Equal(ResourcesLayout.ResourceTab.Graph, cut.Instance.CurrentTab);

        cut.FindAll(".resource-row-link").Single(link => link.GetAttribute("href") == "/resources/api").Click();
        cut.Render();
        Assert.Equal("http://localhost/resources/api", navigation.Uri);
        Assert.Empty(cut.FindComponents<FluentTabs>());
    }

    [Theory]
    [InlineData("/resources?resource=api&resource=worker")]
    [InlineData("/resources?view=graph&resource=api&resource=worker")]
    public void ResourceViewTabs_DisabledGraphOnlyOffersTable(string path)
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources: [ModelTestHelpers.CreateResource("api"), ModelTestHelpers.CreateResource("worker")],
            resourceChannelProvider: () => channel));
        Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>().CurrentValue.UI.DisableResourceGraph = true;
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(path);
        var cut = Render<ResourcesLayout>(builder => builder.AddCascadingValue(viewport));
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsLoaded);
            Assert.Equal(["tab-Overview"],
                cut.FindComponents<FluentTab>().Where(tab => tab.Instance.Visible).Select(tab => tab.Instance.Id));
            Assert.Equal(ResourcesLayout.ResourceTab.Overview, cut.Instance.CurrentTab);
            Assert.Equal("http://localhost/resources?resource=api&resource=worker", navigation.Uri);
        });
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
