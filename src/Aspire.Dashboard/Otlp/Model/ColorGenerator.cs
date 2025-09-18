// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class AccentColor
{
    public AccentColor(string variableName)
    {
        VariableName = variableName;
        ReferencedVariableName = $"var({variableName})";
    }

    public string VariableName { get; }
    public string ReferencedVariableName { get; }
}

public class ColorGenerator
{
    private static readonly string[] s_variableNames =
    [
        "--accent-teal",
        "--accent-marigold",
        "--accent-brass",
        "--accent-peach",
        "--accent-coral",
        "--accent-royal-blue",
        "--accent-orchid",
        "--accent-brand-blue",
        "--accent-seafoam",
        "--accent-mink",
        "--accent-cyan",
        "--accent-gold",
        "--accent-bronze",
        "--accent-orange",
        "--accent-rust",
        "--accent-navy",
        "--accent-berry",
        "--accent-ocean",
        "--accent-jade",
        "--accent-olive"
    ];
    public static readonly ColorGenerator Instance = new ColorGenerator();

    private readonly List<AccentColor> _colors;
    private readonly ConcurrentDictionary<string, Lazy<int>> _colorIndexByKey;
    private int _currentIndex;

    private ColorGenerator()
    {
        _colors = new List<AccentColor>();
        _colorIndexByKey = new ConcurrentDictionary<string, Lazy<int>>(StringComparer.OrdinalIgnoreCase);
        _currentIndex = 0;

        foreach (var name in s_variableNames)
        {
            _colors.Add(new AccentColor(name));
        }
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

    public string GetColorVariableByKey(string key)
    {
        var i = GetColorIndex(key);
        return $"var({_colors[i].VariableName})";
    }

    public void Clear()
    {
        _colorIndexByKey.Clear();
        _currentIndex = 0;
    }
}
