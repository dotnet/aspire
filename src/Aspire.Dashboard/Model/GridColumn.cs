// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public record GridColumn(string Name, Width? DesktopWidth, Width? MobileWidth = null, Func<bool>? IsVisible = null);

public record GridColumnView(string Name, Width Width, Func<bool>? IsVisible = null)
{
    public string? ResolvedBrowserWidth { get; set; }
}

public enum WidthUnit
{
    Pixels,
    Fraction
}

public record struct Width(decimal Value, WidthUnit Unit)
{
    public static Width Pixels(decimal value) => new(value, WidthUnit.Pixels);
    public static Width Fraction(decimal value) => new(value, WidthUnit.Fraction);
}
