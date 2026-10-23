// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
public class ApplicationSummaryTests : DashboardTestContext
{
    [Fact]
    public void Render_DefaultSetting_SummarizesTheAppWithoutListingResources()
    {
        var cut = RenderSummary();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".home-hero")));
        Assert.Equal("1 of 2 resources need attention", cut.Find(".home-hero-headline").TextContent);
        Assert.Single(cut.FindAll(".home-activity"));
        Assert.Single(cut.FindAll(".home-insights"));
        Assert.Equal(["home-errors-title", "home-traces-title"], cut.FindAll(".home-problems h2").Select(h => h.Id));
        // The resource table is the other choice of the "overview of several resources" setting.
        Assert.Empty(cut.FindAll(".home-resource-table"));
    }

    [Fact]
    public async Task Render_CardsSetting_ShowsResourceCardsAndCardActions()
    {
        var cut = RenderSummary();
        await cut.InvokeAsync(() => Services.GetRequiredService<DashboardViewPreferences>().SetGroupOverviewAsync(GroupOverview.Cards));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".resource-card").Count));
        Assert.Empty(cut.FindAll(".home-hero"));
        Assert.Empty(cut.FindAll(".home-activity"));
        Assert.Empty(cut.FindAll(".home-insights"));
        Assert.Empty(cut.FindAll(".home-problems"));
        Assert.Empty(cut.FindAll(".home-resource-table"));
        Assert.Equal(["broken", "api"], cut.FindAll(".resource-card-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(2, cut.FindComponents<ResourceActions>().Count);
        Assert.All(cut.FindComponents<ResourceActions>(), action => Assert.True(action.Instance.CardMode));
    }

    [Fact]
    public async Task Render_CardsSetting_UsesSelectedDensity()
    {
        var cut = RenderSummary();
        var preferences = Services.GetRequiredService<DashboardViewPreferences>();
        await cut.InvokeAsync(() => preferences.SetGroupOverviewAsync(GroupOverview.Cards));

        cut.WaitForAssertion(() => Assert.Contains("resource-card-grid-comfortable", cut.Find(".resource-card-grid").ClassList));

        await cut.InvokeAsync(() => preferences.SetDensityAsync(ResourceListDensity.Compact));
        Assert.Contains("resource-card-grid-compact", cut.Find(".resource-card-grid").ClassList);

        await cut.InvokeAsync(() => preferences.SetDensityAsync(ResourceListDensity.Spacious));
        Assert.Contains("resource-card-grid-spacious", cut.Find(".resource-card-grid").ClassList);
    }

    [Fact]
    public async Task Render_CardsSetting_ParameterCopyActionRequiresResolvedValue()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource(
                    "resolved",
                    resourceType: KnownResourceTypes.Parameter,
                    state: KnownResourceState.Running,
                    properties: new()
                    {
                        [KnownProperties.Parameter.Value] = new(
                            KnownProperties.Parameter.Value,
                            Value.ForString("parameter-value"),
                            isValueSensitive: true,
                            knownProperty: null,
                            sortOrder: 0,
                            displayName: null,
                            isHighlighted: false)
                    }),
                ModelTestHelpers.CreateResource(
                    "unresolved",
                    resourceType: KnownResourceTypes.Parameter,
                    state: KnownResourceState.ValueMissing)
            ],
            resourceChannelProvider: () => channel));
        var cut = Render<ApplicationSummary>(builder => builder.AddCascadingValue(viewport));

        await cut.InvokeAsync(() => Services.GetRequiredService<DashboardViewPreferences>().SetGroupOverviewAsync(GroupOverview.Cards));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".resource-card-parameter").Count));
        var cards = cut.FindAll(".resource-card-parameter");
        Assert.Single(cards.Single(card => card.QuerySelector(".resource-card-title")!.TextContent.Trim() == "resolved").QuerySelectorAll(".resource-card-copy-value"));
        Assert.Empty(cards.Single(card => card.QuerySelector(".resource-card-title")!.TextContent.Trim() == "unresolved").QuerySelectorAll(".resource-card-copy-value"));
        var copyButton = cut.Find(".resource-card-copy-value");
        Assert.Equal("parameter-value", copyButton.GetAttribute("data-text"));
        Assert.Equal("Copy to clipboard", copyButton.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Render_GraphSetting_ShowsTheResourceGraph()
    {
        var module = JSInterop.SetupModule("/js/app-resourcegraph.js");
        module.Mode = JSRuntimeMode.Loose;
        var cut = RenderSummary();

        await cut.InvokeAsync(() => Services.GetRequiredService<DashboardViewPreferences>().SetGroupOverviewAsync(GroupOverview.Graph));

        cut.WaitForAssertion(() => Assert.False(cut.Find(".home-resource-graph-layout").HasAttribute("hidden")));
        Assert.Empty(cut.FindAll(".home-hero"));
        Assert.Empty(cut.FindAll(".home-resource-table"));
        Assert.Single(module.Invocations, invocation => invocation.Identifier == "initializeResourcesGraph");
        Assert.Contains(module.Invocations, invocation => invocation.Identifier == "updateResourcesGraph");
    }

    private IRenderedComponent<ApplicationSummary> RenderSummary()
    {
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        ResourceSetupHelpers.SetupResourcesLayout(this, viewport, new TestDashboardClient(isEnabled: true,
            initialResources:
            [
                ModelTestHelpers.CreateResource("api", state: KnownResourceState.Running, resourceType: "Project"),
                ModelTestHelpers.CreateResource("broken", state: KnownResourceState.FailedToStart, resourceType: "Container")
            ],
            resourceChannelProvider: () => channel));

        // The resource actions take the viewport from the layout.
        return Render<ApplicationSummary>(builder => builder.AddCascadingValue(viewport));
    }
}
