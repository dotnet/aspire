// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Shared;
using Bunit;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public class ParametersTests : DashboardTestContext
{
    [Fact]
    public void ParametersPage_OnlyShowsParameters()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource("Resource1", "Container", "Running", null),
            CreateResource("Param1", KnownResourceTypes.Parameter, "Running", null),
            CreateResource("Resource2", "Project", "Running", null),
            CreateResource("Param2", KnownResourceTypes.Parameter, "Running", null),
        };
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        // Act
        var cut = RenderComponent<Components.Pages.Parameters>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        cut.WaitForState(() => cut.Instance.GetFilteredResources().Any());

        // Assert - only parameters should be shown
        var filteredResources = cut.Instance.GetFilteredResources().ToList();
        Assert.Equal(2, filteredResources.Count);
        Assert.Contains(filteredResources, r => r.Name == "Param1" && r.ResourceType == KnownResourceTypes.Parameter);
        Assert.Contains(filteredResources, r => r.Name == "Param2" && r.ResourceType == KnownResourceTypes.Parameter);
        Assert.DoesNotContain(filteredResources, r => r.ResourceType != KnownResourceTypes.Parameter);
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
            IsHidden = false,
        };
    }
}
