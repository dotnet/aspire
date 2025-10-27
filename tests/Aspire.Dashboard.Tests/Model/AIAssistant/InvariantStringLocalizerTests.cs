// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Aspire.Dashboard.Tests.Model.AIAssistant;

public class InvariantStringLocalizerTests
{
    [Fact]
    public void GetString_DefaultCulture_ExpectedResult()
    {
        // Arrange
        var localizer = new InvariantStringLocalizer<Columns>();

        // Act
        var value = localizer.GetString(nameof(Columns.UnknownStateLabel).ToString());

        // Assert
        Assert.Equal("Unknown", value);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("fr-FR")]
    public void GetString_ManyCultures_ExpectedResult(string culture)
    {
        // Arrange
        var initialCulture = CultureInfo.CurrentCulture;
        var initialUICulture = CultureInfo.CurrentUICulture;
        var newCulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            CultureInfo.CurrentCulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;

            var s = new Columns();
            var localizer = new InvariantStringLocalizer<Columns>();

            // Act
            var value = localizer.GetString(nameof(Columns.UnknownStateLabel).ToString());

            // Assert
            Assert.Equal("Unknown", value);
        }
        finally
        {
            CultureInfo.CurrentCulture = initialCulture;
            CultureInfo.CurrentUICulture = initialUICulture;
        }
    }
}
