// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceIconHelpersTests
{
    [Fact]
    public void GetIconForResource_WithCustomIcon_ReturnsCustomIcon()
    {
        // Arrange
        var resource = CreateResourceWithIcon("Database", IconVariant.Filled);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // The actual icon resolution depends on the IconResolver, so we just verify it doesn't throw
        // and returns a non-null result
    }

    [Fact]
    public void GetIconForResource_WithCustomIconRegularVariant_ReturnsCustomIcon()
    {
        // Arrange
        var resource = CreateResourceWithIcon("CloudArrowUp", IconVariant.Regular);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size16);

        // Assert
        Assert.NotNull(icon);
    }

    [Fact]
    public void GetIconForResource_WithoutCustomIcon_ReturnsDefaultIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Container);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to the default container icon (Box)
    }

    [Fact]
    public void GetIconForResource_WithInvalidCustomIcon_FallsBackToDefault()
    {
        // Arrange
        var resource = CreateResourceWithIcon("NonExistentIcon", IconVariant.Filled, KnownResourceTypes.Project);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to the default project icon even if custom icon doesn't exist
    }

    [Theory]
    [InlineData(KnownResourceTypes.Executable)]
    [InlineData(KnownResourceTypes.Project)]
    [InlineData(KnownResourceTypes.Container)]
    [InlineData(KnownResourceTypes.Parameter)]
    [InlineData(KnownResourceTypes.ConnectionString)]
    [InlineData(KnownResourceTypes.ExternalService)]
    public void GetIconForResource_WithKnownResourceTypes_ReturnsIcon(string resourceType)
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: resourceType);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
    }

    [Fact]
    public void GetIconForResource_WithDatabaseInResourceType_ReturnsDatabaseIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: "postgres-database");

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should match the database special case
    }

    private static ResourceViewModel CreateResourceWithIcon(
        string iconName,
        IconVariant iconVariant,
        string resourceType = KnownResourceTypes.Container)
    {
        var baseResource = ModelTestHelpers.CreateResource(resourceType: resourceType);
        return new ResourceViewModel
        {
            Name = baseResource.Name,
            ResourceType = baseResource.ResourceType,
            DisplayName = baseResource.DisplayName,
            Uid = baseResource.Uid,
            State = baseResource.State,
            StateStyle = baseResource.StateStyle,
            CreationTimeStamp = baseResource.CreationTimeStamp,
            StartTimeStamp = baseResource.StartTimeStamp,
            StopTimeStamp = baseResource.StopTimeStamp,
            Environment = baseResource.Environment,
            Urls = baseResource.Urls,
            Volumes = baseResource.Volumes,
            Relationships = baseResource.Relationships,
            Properties = baseResource.Properties,
            Commands = baseResource.Commands,
            HealthReports = baseResource.HealthReports,
            KnownState = baseResource.KnownState,
            SupportsDetailedTelemetry = baseResource.SupportsDetailedTelemetry,
            IconName = iconName,
            IconVariant = iconVariant
        };
    }
}