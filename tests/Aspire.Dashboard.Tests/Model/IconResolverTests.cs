// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class IconResolverTests
{
    [Fact]
    public void ResolveIconName_ValidIcon_ReturnsIcon()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act
        var icon = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.NotNull(icon);
    }

    [Fact]
    public void ResolveIconName_InvalidIcon_ReturnsNull()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act
        var icon = iconResolver.ResolveIconName("NonExistentIcon", IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.Null(icon);
    }

    [Fact]
    public void ResolveIconName_CachesResults()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act
        var icon1 = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Filled);
        var icon2 = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.NotNull(icon1);
        Assert.NotNull(icon2);
        Assert.Same(icon1, icon2); // Should be the same cached instance
    }

    [Fact]
    public void ResolveIconName_InvalidIcon_CachesNullResult()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act - call twice with same invalid icon
        var icon1 = iconResolver.ResolveIconName("NonExistentIcon", IconSize.Size20, IconVariant.Filled);
        var icon2 = iconResolver.ResolveIconName("NonExistentIcon", IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.Null(icon1);
        Assert.Null(icon2);
        // Both calls should return the same cached null result
    }

    [Fact]
    public void ResolveIconName_DifferentSizes_CreatesSeparateCacheEntries()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act
        var icon16 = iconResolver.ResolveIconName("Database", IconSize.Size16, IconVariant.Filled);
        var icon20 = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Filled);

        // Assert
        Assert.NotNull(icon16);
        Assert.NotNull(icon20);
        // Different sizes should have different instances
        Assert.NotSame(icon16, icon20);
    }

    [Fact]
    public void ResolveIconName_DifferentVariants_CreatesSeparateCacheEntries()
    {
        // Arrange
        var iconResolver = new IconResolver(NullLogger<IconResolver>.Instance);

        // Act
        var iconFilled = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Filled);
        var iconRegular = iconResolver.ResolveIconName("Database", IconSize.Size20, IconVariant.Regular);

        // Assert
        Assert.NotNull(iconFilled);
        Assert.NotNull(iconRegular);
        // Different variants should have different instances
        Assert.NotSame(iconFilled, iconRegular);
    }
}
