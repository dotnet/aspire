// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using Aspire.Dashboard.Model;

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
    // Dark theme colors - more saturated, better contrast on dark backgrounds
    private static readonly string[] s_darkThemeColorsHex =
    [
        "#0078D4", "#107C10", "#D13438", "#F7630C", "#8764B8",
        "#00BCF2", "#13A10E", "#FF4B4B", "#FF8C00", "#744DA9",
        "#40E0FF", "#9FD89F", "#F1AEAD", "#FFB900", "#B4A7D6",
        "#005A9E", "#004B1C", "#A4262C", "#8E562E", "#5B2C6F"
    ];

    // Light theme colors - lighter, more pastel, better contrast on light backgrounds  
    private static readonly string[] s_lightThemeColorsHex =
    [
        "#0F6CBD", "#0E700E", "#C50E16", "#CA5010", "#8B69C7",
        "#038387", "#498205", "#D13438", "#FF8C00", "#8764B8",
        "#0078D4", "#6BB700", "#E81123", "#F7630C", "#881798",
        "#004578", "#0B5A2B", "#A4262C", "#8E562E", "#5B2C6F"
    ];

    public static readonly ColorGenerator Instance = new ColorGenerator();

    private readonly List<GeneratedColor> _darkThemeColors;
    private readonly List<GeneratedColor> _lightThemeColors;
    private readonly ConcurrentDictionary<string, Lazy<int>> _colorIndexByKey;
    private readonly ThemeManager? _themeManager;
    private int _currentIndex;

    // Keep the default constructor for backward compatibility with static Instance
    private ColorGenerator()
    {
        _darkThemeColors = new List<GeneratedColor>();
        _lightThemeColors = new List<GeneratedColor>();
        _colorIndexByKey = new ConcurrentDictionary<string, Lazy<int>>(StringComparer.OrdinalIgnoreCase);
        _currentIndex = 0;

        InitializeColors();
    }

    // Constructor for dependency injection
    public ColorGenerator(ThemeManager themeManager)
    {
        _themeManager = themeManager;
        _darkThemeColors = new List<GeneratedColor>();
        _lightThemeColors = new List<GeneratedColor>();
        _colorIndexByKey = new ConcurrentDictionary<string, Lazy<int>>(StringComparer.OrdinalIgnoreCase);
        _currentIndex = 0;

        InitializeColors();
    }

    private void InitializeColors()
    {
        foreach (var hex in s_darkThemeColorsHex)
        {
            var rgb = GetHexRgb(hex);
            _darkThemeColors.Add(new GeneratedColor
            {
                Hex = hex,
                Red = rgb.Red,
                Green = rgb.Green,
                Blue = rgb.Blue
            });
        }

        foreach (var hex in s_lightThemeColorsHex)
        {
            var rgb = GetHexRgb(hex);
            _lightThemeColors.Add(new GeneratedColor
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
                _currentIndex = ++_currentIndex % Math.Max(_darkThemeColors.Count, _lightThemeColors.Count);
                return i;
            });
        }).Value;
    }

    public string GetColorHexByKey(string key, string? theme = null)
    {
        var i = GetColorIndex(key);
        
        // Determine effective theme
        var effectiveTheme = theme;
        if (effectiveTheme == null && _themeManager != null)
        {
            try
            {
                effectiveTheme = _themeManager.EffectiveTheme;
            }
            catch (InvalidOperationException)
            {
                // ThemeManager not initialized, fall back to dark theme
                effectiveTheme = ThemeManager.ThemeSettingDark;
            }
        }
        
        var colors = effectiveTheme == ThemeManager.ThemeSettingLight ? _lightThemeColors : _darkThemeColors;
        return colors[i % colors.Count].Hex;
    }

    // Overload for backward compatibility
    public string GetColorHexByKey(string key)
    {
        return GetColorHexByKey(key, null);
    }

    public void Clear()
    {
        _colorIndexByKey.Clear();
        _currentIndex = 0;
    }
}
