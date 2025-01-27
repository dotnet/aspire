// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Bunit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class ResourcesTests : TestContext
{
    [Fact]
    public void Test()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            [
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
            ]);

        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        // Open the resource filter
        cut.Find("#resourceFilterButton").Click();

        // Assert 1 (the correct filter options are shown)
        var filterComponents = cut.FindComponents<SelectResourceOptions<string>>();
        Assert.Equal(3, filterComponents.Count);

        var typeSelect = filterComponents.First(f => f.Instance.Id == "resource-types");
        Assert.Equal([
            new("Type1", true),
            new("Type2", true),
            new("Type3", true),
        ], typeSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */ );

        var stateSelect = filterComponents.First(f => f.Instance.Id == "resource-states");
        Assert.Equal([
            new("Running", true),
            new("Stopping", true),
        ], stateSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);

        var healthSelect = filterComponents.First(f => f.Instance.Id == "resource-health-states");
        Assert.Equal([
            new("", true),
            new("Healthy", true),
            new("Unhealthy", true),
        ], healthSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);
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
        };
    }
}
