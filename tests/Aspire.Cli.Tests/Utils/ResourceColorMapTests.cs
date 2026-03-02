// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class ResourceColorMapTests
{
    [Fact]
    public void HexColorsCountMatchesVariableNamesCount()
    {
        Assert.Equal(ColorGenerator.s_variableNames.Length, ResourceColorMap.s_hexColors.Length);
    }

    [Fact]
    public void AllHexColorsAreValidFormat()
    {
        foreach (var hex in ResourceColorMap.s_hexColors)
        {
            Assert.Matches(@"^#[0-9A-Fa-f]{6}$", hex);
        }
    }

    [Fact]
    public void GetColorReturnsDeterministicResult()
    {
        var map = new ResourceColorMap();
        var color1 = map.GetColor("test-resource");
        var color2 = map.GetColor("test-resource");

        Assert.Equal(color1, color2);
    }

    [Fact]
    public void ResolveAllMakesColorAssignmentDeterministic()
    {
        var map1 = new ResourceColorMap();
        map1.ResolveAll(["bravo", "alpha", "charlie"]);

        var map2 = new ResourceColorMap();
        map2.ResolveAll(["charlie", "alpha", "bravo"]);

        Assert.Equal(map1.GetColor("alpha"), map2.GetColor("alpha"));
        Assert.Equal(map1.GetColor("bravo"), map2.GetColor("bravo"));
        Assert.Equal(map1.GetColor("charlie"), map2.GetColor("charlie"));
    }

    [Fact]
    public void DifferentNamesGetDifferentColors()
    {
        var map = new ResourceColorMap();
        var color1 = map.GetColor("resource-a");
        var color2 = map.GetColor("resource-b");

        Assert.NotEqual(color1, color2);
    }
}
