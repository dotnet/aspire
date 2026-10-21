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
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
public class ApplicationSummaryTests : DashboardTestContext
{
    [Fact]
    public void Render_SummarizesTheAppWithoutListingResources()
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
    public async Task Render_ResourcesSetting_ShowsOnlyTheResourceTable()
    {
        var cut = RenderSummary();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".home-hero")));

        // The setting applies right away, without reloading the page.
        await cut.InvokeAsync(() => Services.GetRequiredService<DashboardViewPreferences>().SetGroupOverviewAsync(GroupOverview.Resources));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".home-resource-table")));
        Assert.Empty(cut.FindAll(".home-hero"));
        Assert.Empty(cut.FindAll(".home-activity"));
        Assert.Empty(cut.FindAll(".home-insights"));
        Assert.Empty(cut.FindAll(".home-problems"));
        // The same columns and actions as the resources grid, ordered by type then name.
        Assert.Equal(
            ["Name", "State", "Start time", "Type", "Source", "URLs", "Actions"],
            cut.FindAll(".home-resource-table thead th").Select(th => th.TextContent));
        Assert.Equal(
            ["Container", "Project"],
            cut.FindAll(".home-resource-table tbody .home-table-type").Select(type => type.TextContent));
        Assert.Equal(2, cut.FindComponents<ResourceActions>().Count);
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
