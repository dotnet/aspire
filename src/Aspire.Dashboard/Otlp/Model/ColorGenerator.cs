// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class GeneratedColor
{
    public required string Hex { get; init; }
    public required int Red { get; init; }
    public required int Green { get; init; }
    public required int Blue { get; init; }
}

public class ColorGenerator
{
    private static readonly string[] s_colorsHex =
    [
        "#0078D4", "#107C10", "#D13438", "#F7630C", "#8764B8",
        "#00BCF2", "#13A10E", "#FF4B4B", "#FF8C00", "#744DA9",
        "#40E0FF", "#9FD89F", "#F1AEAD", "#FFB900", "#B4A7D6",
        "#005A9E", "#004B1C", "#A4262C", "#8E562E", "#5B2C6F"
    ];
    public static readonly ColorGenerator Instance = new ColorGenerator();

    private readonly List<GeneratedColor> _colors;
    private readonly ConcurrentDictionary<string, Lazy<int>> _colorIndexByKey;
    private int _currentIndex;

    private ColorGenerator()
    {
        _colors = new List<GeneratedColor>();
        _colorIndexByKey = new ConcurrentDictionary<string, Lazy<int>>(StringComparer.OrdinalIgnoreCase);
        _currentIndex = 0;

        foreach (var hex in s_colorsHex)
        {
            var rgb = GetHexRgb(hex);
            _colors.Add(new GeneratedColor
            {
                Hex = hex,
                Red = rgb.Red,
                Green = rgb.Green,
                Blue = rgb.Blue
            });
        }
    }

    private static (int Red, int Green, int Blue) GetHexRgb(string s)
    {
        if (s.Length != 7)
        {
            return (0, 0, 0);
        }

        var r = int.Parse(s.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = int.Parse(s.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = int.Parse(s.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return (r, g, b);
    }

    private int GetColorIndex(string key)
    {
        return _colorIndexByKey.GetOrAdd(key, k =>
        {
            // GetOrAdd is run outside of the lock.
            // Use lazy to ensure that the index is only calculated once for an app.
            return new Lazy<int>(() =>
            {
                var i = _currentIndex;
                _currentIndex = ++_currentIndex % _colors.Count;
                return i;
            });
        }).Value;
    }

    public string GetColorHexByKey(string key)
    {
        var i = GetColorIndex(key);
        return _colors[i].Hex;
    }

    public void Clear()
    {
        _colorIndexByKey.Clear();
        _currentIndex = 0;
    }
}
