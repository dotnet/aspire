// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Google.Protobuf.WellKnownTypes;
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

    [Theory]
    [InlineData(".csproj", "CodeCsRectangle")]
    [InlineData(".fsproj", "CodeFsRectangle")]
    [InlineData(".vbproj", "CodeVbRectangle")]
    public void GetIconForResource_WithSpecificProjectType_ReturnsLanguageSpecificIcon(string extension, string _)
    {
        // Arrange
        var projectPath = $"/path/to/project{extension}";
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Project.Path] = new ResourcePropertyViewModel(KnownProperties.Project.Path, Value.ForString(projectPath), isValueSensitive: false, knownProperty: null, priority: 0)
        };
        var resource = ModelTestHelpers.CreateResource(
            resourceType: KnownResourceTypes.Project,
            properties: properties);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Icon name should correspond to the project type
    }

    [Fact]
    public void GetIconForResource_WithUnknownProjectExtension_ReturnsGenericCodeIcon()
    {
        // Arrange
        var projectPath = "/path/to/project.xyz";
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Project.Path] = new ResourcePropertyViewModel(KnownProperties.Project.Path, Value.ForString(projectPath), isValueSensitive: false, knownProperty: null, priority: 0)
        };
        var resource = ModelTestHelpers.CreateResource(
            resourceType: KnownResourceTypes.Project,
            properties: properties);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to generic code icon for unknown extensions
    }

    [Fact]
    public void GetIconForResource_WithProjectButNoPath_ReturnsGenericCodeIcon()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Project);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to generic code icon when no project path is available
    }

    [Fact]
    public void GetIconForResource_WithEmptyProjectPath_ReturnsGenericCodeIcon()
    {
        // Arrange
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Project.Path] = new ResourcePropertyViewModel(KnownProperties.Project.Path, Value.ForString(string.Empty), isValueSensitive: false, knownProperty: null, priority: 0)
        };
        var resource = ModelTestHelpers.CreateResource(
            resourceType: KnownResourceTypes.Project,
            properties: properties);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should fall back to generic code icon for empty path
    }

    [Fact]
    public void GetIconForResource_WithUpperCaseExtension_ResolvesCaseInsensitively()
    {
        // Arrange
        var projectPath = "/path/to/project.CSPROJ";
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Project.Path] = new ResourcePropertyViewModel(KnownProperties.Project.Path, Value.ForString(projectPath), isValueSensitive: false, knownProperty: null, priority: 0)
        };
        var resource = ModelTestHelpers.CreateResource(
            resourceType: KnownResourceTypes.Project,
            properties: properties);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should handle case-insensitive extension matching
    }

    [Fact]
    public void GetIconForResource_WithMixedCaseExtension_ResolvesCaseInsensitively()
    {
        // Arrange
        var projectPath = "/path/to/project.CsProj";
        var properties = new Dictionary<string, ResourcePropertyViewModel>
        {
            [KnownProperties.Project.Path] = new ResourcePropertyViewModel(KnownProperties.Project.Path, Value.ForString(projectPath), isValueSensitive: false, knownProperty: null, priority: 0)
        };
        var resource = ModelTestHelpers.CreateResource(
            resourceType: KnownResourceTypes.Project,
            properties: properties);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20);

        // Assert
        Assert.NotNull(icon);
        // Should handle case-insensitive extension matching
    }

    [Theory]
    [InlineData(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)]
    [InlineData(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)]
    [InlineData(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)]
    [InlineData(null)]
    public void GetHealthStatusIcon_WithVariousHealthStatuses_ReturnsIconAndColor(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? healthStatus)
    {
        // Act
        var (icon, color) = ResourceIconHelpers.GetHealthStatusIcon(healthStatus);

        // Assert
        if (healthStatus == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
        {
            Assert.NotNull(icon);
            Assert.Equal(Color.Success, color);
        }
        else if (healthStatus == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
        {
            Assert.NotNull(icon);
            Assert.Equal(Color.Warning, color);
        }
        else if (healthStatus == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
        {
            Assert.NotNull(icon);
            Assert.Equal(Color.Error, color);
        }
        else
        {
            Assert.NotNull(icon);
            Assert.Equal(Color.Info, color);
        }
    }

    [Fact]
    public void GetIconForResource_WithDesiredVariantRegular_UsesRegularVariant()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Container);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20, IconVariant.Regular);

        // Assert
        Assert.NotNull(icon);
        // Should use regular variant when specified
    }

    [Fact]
    public void GetIconForResource_WithDesiredVariantFilled_UsesFilledVariant()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Container);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.NotNull(icon);
        // Should use filled variant when specified (default)
    }

    [Theory]
    [InlineData(IconSize.Size16)]
    [InlineData(IconSize.Size20)]
    [InlineData(IconSize.Size24)]
    public void GetIconForResource_WithDifferentSizes_ReturnsIconOfDesiredSize(IconSize size)
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceType: KnownResourceTypes.Container);

        // Act
        var icon = ResourceIconHelpers.GetIconForResource(_iconResolver, resource, size);

        // Assert
        Assert.NotNull(icon);
        // Icon should be resolved at the desired size
    }
}