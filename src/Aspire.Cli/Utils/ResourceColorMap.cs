// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Assigns a consistent color to each resource name for colorized console output.
/// </summary>
internal sealed class ResourceColorMap
{
    private static readonly Color[] s_resourceColors =
    [
        Color.Cyan1,
        Color.Green,
        Color.Yellow,
        Color.Blue,
        Color.Magenta1,
        Color.Orange1,
        Color.DeepPink1,
        Color.SpringGreen1,
        Color.Aqua,
        Color.Violet
    ];

    private readonly Dictionary<string, Color> _colorMap = new(StringComparers.ResourceName);
    private int _nextColorIndex;

    /// <summary>
    /// Gets the color assigned to the specified resource name, assigning a new one if first seen.
    /// </summary>
    public Color GetColor(string resourceName)
    {
        if (!_colorMap.TryGetValue(resourceName, out var color))
        {
            color = s_resourceColors[_nextColorIndex % s_resourceColors.Length];
            _colorMap[resourceName] = color;
            _nextColorIndex++;
        }
        return color;
    }
}
