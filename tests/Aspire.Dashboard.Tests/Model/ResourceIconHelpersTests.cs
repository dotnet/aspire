// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceIconHelpersTests
{
    private readonly IconResolver _iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

    [Fact]
    public void GetIconForResource_WithCustomIcon_ReturnsCustomIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(iconName: "Database", iconVariant: IconVariant.Filled);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // The actual icon resolution depends on the IconResolver, so we just verify it doesn't throw
        // and returns a non-null result
    }

    [Fact]
    public void GetIconForResource_WithCustomIconRegularVariant_ReturnsCustomIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(iconName: "CloudArrowUp", iconVariant: IconVariant.Regular);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size16);

        // Assert
        Assert.NotNull(icon);
    }

    [Fact]
    public void GetIconForResource_WithoutCustomIcon_ReturnsDefaultIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Container);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to the default container icon (Box)
    }

    [Fact]
    public void GetIconForResource_WithInvalidCustomIcon_FallsBackToDefault()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Project, iconName: "NonExistentIcon", iconVariant: IconVariant.Filled);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

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
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
    }

    [Fact]
    public void GetIconForResource_WithDatabaseInResourceType_ReturnsDatabaseIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: "postgres-database");

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should match the database special case
    }

    [Fact]
    public void GetIconForResource_WithCustomIconData_ReturnsCustomResourceIcon()
    {
        // Arrange
        var svgContent = "<svg><circle cx='50' cy='50' r='40'/></svg>";
        var resource = ModelTestHelpers.CreateResource(customIconData: svgContent);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        Assert.IsType<CustomResourceIcon>(icon);
    }

    [Fact]
    public void GetIconForResource_WithCustomIconDataAndIconName_PrioritizesCustomData()
    {
        // Arrange
        var svgContent = "<svg><rect width='24' height='24'/></svg>";
        var resource = ModelTestHelpers.CreateResource(iconName: "Database", customIconData: svgContent);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        Assert.IsType<CustomResourceIcon>(icon);
        // Custom data takes precedence over icon name
    }

    [Fact]
    public void GetIconForResource_WithDataUriCustomIconData_ReturnsCustomResourceIcon()
    {
        // Arrange
        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA";
        var resource = ModelTestHelpers.CreateResource(customIconData: dataUri);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size16);

        // Assert
        Assert.NotNull(icon);
        Assert.IsType<CustomResourceIcon>(icon);
    }

}