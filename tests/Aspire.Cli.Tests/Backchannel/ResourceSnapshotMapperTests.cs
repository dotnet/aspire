// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Tests.Backchannel;

public class ResourceSnapshotMapperTests
{
    [Fact]
    public void MapToResourceJson_WithPopulatedProperties_MapsCorrectly()
    {
        // Arrange
        var snapshot = new ResourceSnapshot
        {
            Name = "frontend",
            DisplayName = "frontend",
            ResourceType = "Project",
            State = "Running",
            Urls =
            [
                new ResourceSnapshotUrl { Name = "http", Url = "http://localhost:5000" }
            ],
            Commands =
            [
                new ResourceSnapshotCommand { Name = "resource-stop", State = "Enabled", Description = "Stop" },
                new ResourceSnapshotCommand { Name = "resource-start", State = "Disabled", Description = "Start" }
            ],
            EnvironmentVariables =
            [
                new ResourceSnapshotEnvironmentVariable { Name = "ASPNETCORE_ENVIRONMENT", Value = "Development", IsFromSpec = true },
                new ResourceSnapshotEnvironmentVariable { Name = "INTERNAL_VAR", Value = "hidden", IsFromSpec = false }
            ]
        };

        var allSnapshots = new List<ResourceSnapshot> { snapshot };

        // Act
        var result = ResourceSnapshotMapper.MapToResourceJson(snapshot, allSnapshots, dashboardBaseUrl: "http://localhost:18080");

        // Assert
        Assert.Equal("frontend", result.Name);
        Assert.Single(result.Urls!);
        Assert.Equal("http://localhost:5000", result.Urls![0].Url);

        // Only enabled commands should be included
        Assert.Single(result.Commands!);
        Assert.True(result.Commands!.ContainsKey("resource-stop"));

        // Only IsFromSpec environment variables should be included
        Assert.Single(result.Environment!);
        Assert.Equal("Development", result.Environment!["ASPNETCORE_ENVIRONMENT"]);

        // Dashboard URL should be generated
        Assert.NotNull(result.DashboardUrl);
        Assert.Contains("localhost:18080", result.DashboardUrl);
    }

}
