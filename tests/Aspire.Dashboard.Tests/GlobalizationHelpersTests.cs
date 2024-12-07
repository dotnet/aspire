// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class GlobalizationHelpersTests
{
    [Fact]
    public void GetSupportedCultures_IncludesPopularCultures()
    {
        // Act
        var supportedCultures = GlobalizationHelpers.GetSupportedCultures();

        // Assert
        foreach (var localizedCulture in GlobalizationHelpers.LocalizedCultures)
        {
            Assert.Contains(localizedCulture, supportedCultures);
        }

        // A few cultures we expect to be available
        Assert.Contains("en-GB", supportedCultures);
        Assert.Contains("fr-CA", supportedCultures);
        Assert.Contains("zh-CN", supportedCultures);
    }

    [Theory]
    [InlineData("en", true, "en")]
    [InlineData("en-US", true, "en")]
    [InlineData("fr", true, "fr")]
    [InlineData("zh-Hans", true, "zh-Hans")]
    [InlineData("zh-Hant", true, "zh-Hant")]
    [InlineData("zh-CN", false, null)]
    [InlineData("es", false, null)]
    public void TryGetCulture_VariousCultures_ReturnsExpectedResult(string cultureName, bool expectedResult, string? expectedMatchedCultureName)
    {
        // Arrange
        var cultureOptions = new HashSet<CultureInfo>
        {
            new("en"),
            new("fr"),
            new("zh-Hans"),
            new("zh-Hant")
        };
        var culture = new CultureInfo(cultureName);

        // Act
        var result = cultureOptions.TryGetCulture(culture, matchParent: true, out var matchedCulture);

        // Assert
        Assert.Equal(expectedResult, result);
        if (expectedMatchedCultureName is null)
        {
            Assert.Null(matchedCulture);
        }
        else
        {
            Assert.Equal(new CultureInfo(expectedMatchedCultureName), matchedCulture);
        }
    }
}
