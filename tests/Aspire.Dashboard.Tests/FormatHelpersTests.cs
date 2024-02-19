// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class FormatHelpersTests
{
    [Theory]
    [InlineData("9", 9d)]
    [InlineData("9.9", 9.9d)]
    [InlineData("0.9", 0.9d)]
    [InlineData("12,345,678.9", 12345678.9d)]
    public void FormatNumberWithOptionalDecimalPlaces_InvariantCulture(string expected, double value)
    {
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("9", 9d)]
    [InlineData("9,9", 9.9d)]
    [InlineData("0,9", 0.9d)]
    [InlineData("12345678,9", 12345678.9d)]
    public void FormatNumberWithOptionalDecimalPlaces_FrenchCulture(string expected, double value)
    {
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, CultureInfo.GetCultureInfo("fr-FR")));
    }
}
