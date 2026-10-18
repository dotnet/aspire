// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components;
using Aspire.Dashboard.Components.Controls.Chart;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class SparklinePathTests
{
    [Fact]
    public void Build_NoValues_ReturnsEmptyPaths()
    {
        var (line, area) = SparklinePath.Build([null, null, null], width: 100, height: 32);

        Assert.Equal(string.Empty, line);
        Assert.Equal(string.Empty, area);
    }

    [Fact]
    public void Build_FlatValues_DrawsLineThroughMiddle()
    {
        var (line, area) = SparklinePath.Build([5, 5, 5], width: 100, height: 32);

        Assert.Equal("M0,16C16.67,16 33.33,16 50,16C66.67,16 83.33,16 100,16", line);
        Assert.Equal(line + "L100,32L0,32Z", area);
    }

    [Fact]
    public void Build_ScalesToDataRange()
    {
        // Values span 10..20, so 20 maps to the top inset (2) and 10 to the bottom inset (30).
        var (line, _) = SparklinePath.Build([10, 20], width: 100, height: 32);

        Assert.Equal("M0,30C33.33,20.67 66.67,11.33 100,2", line);
    }

    [Fact]
    public void Build_GapInValues_StartsNewSegment()
    {
        var (line, area) = SparklinePath.Build([1, 2, null, 2, 1], width: 100, height: 32);

        Assert.Equal("M0,30C8.33,20.67 16.67,11.33 25,2M75,2C83.33,11.33 91.67,20.67 100,30", line);
        Assert.Equal(
            "M0,30C8.33,20.67 16.67,11.33 25,2L25,32L0,32Z" +
            "M75,2C83.33,11.33 91.67,20.67 100,30L100,32L75,32Z",
            area);
    }

    [Fact]
    public void Build_StepChange_DoesNotOvershoot()
    {
        // Monotone interpolation keeps every control point within the data's vertical range,
        // so a step from 0 to 10 never draws a bump above the top or below the bottom.
        var (line, _) = SparklinePath.Build([0, 0, 0, 10, 10, 10], width: 100, height: 32);

        var coordinates = line
            .Split(['M', 'C', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(point => double.Parse(point.Split(',')[1], CultureInfo.InvariantCulture))
            .ToList();

        Assert.All(coordinates, y => Assert.InRange(y, 2, 30));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(0.0042, "0.004")]
    [InlineData(999.5, "999.5")]
    [InlineData(9999, "9,999")]
    [InlineData(12345, "12.3K")]
    [InlineData(125300000, "125.3M")]
    [InlineData(-2500000, "-2.5M")]
    [InlineData(7.2e9, "7.2B")]
    [InlineData(3e12, "3T")]
    public void FormatCompactValue_FormatsWithSuffix(double value, string expected)
    {
        Assert.Equal(expected, MeterOverview.FormatCompactValue(value, CultureInfo.InvariantCulture));
    }
}
