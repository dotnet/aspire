// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Utils;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class ResourcesTests : DashboardTestContext
{
    [Fact]
    public void UpdateResources_FiltersUpdated()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource(
                "Resource1",
                "Type1",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Null", null, "Description1", null))),
        };
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: () => channel);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        // Assert 1
        Assert.Collection(cut.Instance.PageViewModel.ResourceTypesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Type1", kvp.Key);
                Assert.True(kvp.Value);
            });
        Assert.Collection(cut.Instance.PageViewModel.ResourceStatesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Running", kvp.Key);
                Assert.True(kvp.Value);
            });
        Assert.Collection(cut.Instance.PageViewModel.ResourceHealthStatusesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Unhealthy", kvp.Key);
                Assert.True(kvp.Value);
            });

        // Act
        channel.Writer.TryWrite([
            new ResourceViewModelChange(
                ResourceViewModelChangeType.Upsert,
                CreateResource(
                    "Resource2",
                    "Type2",
                    "Running",
                    ImmutableArray.Create(new HealthReportViewModel("Healthy", HealthStatus.Healthy, "Description2", null))))
            ]);

        cut.WaitForState(() => cut.Instance.GetFilteredResources().Count() == 2);

        // Assert 2
        Assert.Collection(cut.Instance.PageViewModel.ResourceTypesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Type1", kvp.Key);
                Assert.True(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Type2", kvp.Key);
                Assert.True(kvp.Value);
            });
        Assert.Collection(cut.Instance.PageViewModel.ResourceStatesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Running", kvp.Key);
                Assert.True(kvp.Value);
            });
        Assert.Collection(cut.Instance.PageViewModel.ResourceHealthStatusesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Healthy", kvp.Key);
                Assert.True(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Unhealthy", kvp.Key);
                Assert.True(kvp.Value);
            });
    }

    [Fact]
    public void FilterResources()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource(
                "Resource1",
                "Type1",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Null", null, "Description1", null))),
            CreateResource(
                "Resource2",
                "Type2",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Healthy", HealthStatus.Healthy, "Description2", null))),
            CreateResource(
                "Resource3",
                "Type3",
                "Stopping",
                ImmutableArray.Create(new HealthReportViewModel("Degraded", HealthStatus.Degraded, "Description3", null))),
        };
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        // Open the resource filter
        cut.Find("#resourceFilterButton").Click();

        // Assert 1 (the correct filter options are shown)
        AssertResourceFilterListEquals(cut, [
            new("Type1", true),
            new("Type2", true),
            new("Type3", true),
        ], [
            new("Running", true),
            new("Stopping", true),
        ], [
            new("", true),
            new("Healthy", true),
            new("Unhealthy", true),
        ]);

        // Assert 2 (unselect a resource type, assert that a resource was removed)
        cut.FindComponents<SelectResourceOptions<string>>().First(f => f.Instance.Id == "resource-states")
            .FindComponents<FluentCheckbox>()
            .First(checkbox => checkbox.Instance.Label == "Stopping")
            .Find("fluent-checkbox")
            .TriggerEvent("oncheckedchange", new CheckboxChangeEventArgs { Checked = false });

        // above is triggered asynchronously, so wait for the state to change
        cut.WaitForState(() => cut.Instance.GetFilteredResources().Count() == 2);
    }

    [Fact]
    public void ResourceGraph_MultipleRenders_InitializeOnce()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource(
                "Resource1",
                "Type1",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Null", null, "Description1", null))),
        };
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        var resourceGraphModule = JSInterop.SetupModule("/js/app-resourcegraph.js");
        var initializeGraphInvocationHandler = resourceGraphModule.SetupVoid("initializeResourcesGraph", _ => true);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(DashboardUrls.ResourcesUrl(view: "Graph"));

        // Act
        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        cut.Render();

        // Assert
        Assert.Single(initializeGraphInvocationHandler.Invocations);
    }

    [Fact]
    public void ResourceFilters_ApplyExistingFiltersOnInitialRender()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource("Resource1", "Type1", "Running", null),
            CreateResource("Resource2", "Type2", "Finished", null),
        };

        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources,
            resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>);
        ResourceSetupHelpers.SetupResourcesPage(this, viewport, dashboardClient);

        var sessionStorage = (TestSessionStorage)Services.GetRequiredService<ISessionStorage>();
        // Simulate existing filters in session storage
        sessionStorage.OnGetAsync = key =>
        {
            if (key is BrowserStorageKeys.ResourcesPageState)
            {
                return (true,
                    new Components.Pages.Resources.ResourcesPageState
                    {
                        ResourceTypesToVisibility =
                            new Dictionary<string, bool> { { "Type1", true }, { "Type2", false } },
                        ResourceStatesToVisibility =
                            new Dictionary<string, bool> { { "Running", true }, { "Finished", false } },
                        ResourceHealthStatusesToVisibility =
                            new Dictionary<string, bool> { { "Healthy", true }, { "Unhealthy", false } },
                        ViewKind = null,
                    });
            }

            return (false, null);
        };

        // Act and assert
        var cut = RenderComponent<Components.Pages.Resources>(builder => { builder.AddCascadingValue(viewport); });

        Assert.Collection(cut.Instance.PageViewModel.ResourceTypesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Type1", kvp.Key);
                Assert.True(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Type2", kvp.Key);
                Assert.False(kvp.Value);
            });
        Assert.Collection(cut.Instance.PageViewModel.ResourceStatesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Finished", kvp.Key);
                Assert.False(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Running", kvp.Key);
                Assert.True(kvp.Value);
            });

        // Unhealthy not included because it's not present in any resource
        Assert.Collection(cut.Instance.PageViewModel.ResourceHealthStatusesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal(string.Empty, kvp.Key);
                Assert.True(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Healthy", kvp.Key);
                Assert.True(kvp.Value);
            });
    }

    private static void AssertResourceFilterListEquals(IRenderedComponent<Components.Pages.Resources> cut, IEnumerable<KeyValuePair<string, bool>> types, IEnumerable<KeyValuePair<string, bool>> states, IEnumerable<KeyValuePair<string, bool>> healthStates)
    {
        IReadOnlyList<IRenderedComponent<SelectResourceOptions<string>>> filterComponents = null!;

        cut.WaitForState(() =>
        {
            filterComponents = cut.FindComponents<SelectResourceOptions<string>>();
            return filterComponents.Count == 3;
        });

        var typeSelect = filterComponents.First(f => f.Instance.Id == "resource-types");
        Assert.Equal(types, typeSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */ );

        var stateSelect = filterComponents.First(f => f.Instance.Id == "resource-states");
        Assert.Equal(states, stateSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);

        var healthSelect = filterComponents.First(f => f.Instance.Id == "resource-health-states");
        Assert.Equal(healthStates, healthSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);
    }

    private static ResourceViewModel CreateResource(string name, string type, string? state, ImmutableArray<HealthReportViewModel>? healthReports)
    {
        return new ResourceViewModel
        {
            Name = name,
            ResourceType = type,
            State = state,
            KnownState = state is not null ? Enum.Parse<KnownResourceState>(state) : null,
            DisplayName = name,
            Uid = name,
            HealthReports = healthReports ?? [],

            // unused properties
            StateStyle = null,
            CreationTimeStamp = null,
            StartTimeStamp = null,
            StopTimeStamp = null,
            Environment = default,
            Urls = [],
            Volumes = default,
            Relationships = default,
            Properties = ImmutableDictionary<string, ResourcePropertyViewModel>.Empty,
            Commands = [],
            Hidden = false,
        };
    }
}
