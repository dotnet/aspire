// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Tray;

/// <summary>A straight (not premultiplied) sRGB color.</summary>
internal readonly record struct PanelColor(byte R, byte G, byte B, byte A = 255)
{
    public double Alpha => A / 255.0;

    /// <summary>Parses "#RRGGBB" or "#RRGGBBAA".</summary>
    public static PanelColor FromHex(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');
        if (span.Length is not (6 or 8))
        {
            throw new FormatException($"'{hex}' is not a #RRGGBB or #RRGGBBAA color.");
        }
        var value = uint.Parse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return span.Length == 6
            ? new((byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : new((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    public static PanelColor FromRgba(byte r, byte g, byte b, double alpha) => new(r, g, b, (byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255));

    public PanelColor WithAlpha(double alpha) => this with { A = (byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255) };

    /// <summary>Composites this color over an opaque background, which GDI text needs because it cannot blend.</summary>
    public PanelColor Over(PanelColor background)
    {
        var a = Alpha;
        return new(
            (byte)Math.Round(R * a + background.R * (1 - a)),
            (byte)Math.Round(G * a + background.G * (1 - a)),
            (byte)Math.Round(B * a + background.B * (1 - a)));
    }

    public string ToHex() => A == 255
        ? string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}{A:X2}");
}

/// <summary>
/// The resolved colors of the panel. Both platforms pick one of these and pass it to the
/// shared scene, so a theme is defined once and every renderer draws the same values.
/// </summary>
internal sealed record TrayPanelPalette(
    string Name,
    PanelColor Background,
    PanelColor Gutter,
    PanelColor Separator,
    PanelColor Border,
    PanelColor TextPrimary,
    PanelColor TextSecondary,
    PanelColor TextTertiary,
    PanelColor Hover,
    PanelColor Pressed,
    PanelColor Accent,
    PanelColor Success,
    PanelColor Warning,
    PanelColor Danger,
    PanelColor Neutral,
    PanelColor Banner,
    PanelColor BannerText,
    PanelColor OnColor)
{
    /// <summary>The color of the keyboard focus ring.</summary>
    public PanelColor Focus => Accent;

    public static TrayPanelPalette Light { get; } = new(
        "light",
        Background: PanelColor.FromHex("#F9F9FB"),
        Gutter: PanelColor.FromHex("#EFEFF3"),
        Separator: PanelColor.FromRgba(0, 0, 0, 0.08),
        Border: PanelColor.FromRgba(0, 0, 0, 0.12),
        TextPrimary: PanelColor.FromHex("#1C1C1E"),
        TextSecondary: PanelColor.FromHex("#5F5F66"),
        TextTertiary: PanelColor.FromHex("#77777F"),
        Hover: PanelColor.FromRgba(0, 0, 0, 0.05),
        Pressed: PanelColor.FromRgba(0, 0, 0, 0.09),
        Accent: PanelColor.FromHex("#6A4BD6"),
        Success: PanelColor.FromHex("#1E9E57"),
        Warning: PanelColor.FromHex("#D98A00"),
        Danger: PanelColor.FromHex("#D93A3A"),
        Neutral: PanelColor.FromHex("#8E8E96"),
        Banner: PanelColor.FromHex("#FFF4DB"),
        BannerText: PanelColor.FromHex("#6B4500"),
        OnColor: PanelColor.FromHex("#FFFFFF"));

    public static TrayPanelPalette Dark { get; } = new(
        "dark",
        Background: PanelColor.FromHex("#252529"),
        Gutter: PanelColor.FromHex("#1E1E21"),
        Separator: PanelColor.FromRgba(255, 255, 255, 0.08),
        Border: PanelColor.FromRgba(255, 255, 255, 0.12),
        TextPrimary: PanelColor.FromHex("#F2F2F5"),
        TextSecondary: PanelColor.FromHex("#A8A8B0"),
        TextTertiary: PanelColor.FromHex("#8C8C95"),
        Hover: PanelColor.FromRgba(255, 255, 255, 0.07),
        Pressed: PanelColor.FromRgba(255, 255, 255, 0.11),
        Accent: PanelColor.FromHex("#9C85F2"),
        Success: PanelColor.FromHex("#34C46E"),
        Warning: PanelColor.FromHex("#F2A93B"),
        Danger: PanelColor.FromHex("#F2645F"),
        Neutral: PanelColor.FromHex("#8E8E96"),
        Banner: PanelColor.FromHex("#3A2F17"),
        BannerText: PanelColor.FromHex("#F7D9A0"),
        OnColor: PanelColor.FromHex("#FFFFFF"));

    /// <summary>
    /// Returns the palette for the macOS "Increase contrast" accessibility setting: secondary
    /// text moves toward primary text and separators and borders become clearly visible.
    /// </summary>
    public TrayPanelPalette WithIncreasedContrast() => this with
    {
        Name = Name + "-contrast",
        TextSecondary = TextPrimary.WithAlpha(0.85).Over(Background),
        TextTertiary = TextPrimary.WithAlpha(0.75).Over(Background),
        Separator = TextPrimary.WithAlpha(0.35),
        Border = TextPrimary.WithAlpha(0.6),
        Hover = TextPrimary.WithAlpha(0.12),
        Pressed = TextPrimary.WithAlpha(0.2)
    };

    /// <summary>
    /// Builds the Windows high-contrast palette from system colors (GetSysColor), so the panel
    /// follows the user's contrast theme rather than the brand colors. Status shapes still differ
    /// by state, so the meaning does not depend on color.
    /// https://learn.microsoft.com/windows/win32/winauto/high-contrast-parameter
    /// </summary>
    public static TrayPanelPalette FromSystemColors(PanelColor window, PanelColor windowText, PanelColor highlight,
        PanelColor grayText) => new(
        "high-contrast",
        Background: window,
        Gutter: window,
        Separator: windowText,
        Border: windowText,
        TextPrimary: windowText,
        TextSecondary: windowText,
        TextTertiary: windowText,
        Hover: highlight.WithAlpha(0.35),
        Pressed: highlight.WithAlpha(0.55),
        Accent: highlight,
        Success: windowText,
        Warning: windowText,
        Danger: windowText,
        Neutral: grayText,
        Banner: window,
        BannerText: windowText,
        OnColor: window)
    {
        IsHighContrast = true
    };

    /// <summary>True for a system high-contrast palette, where the banner also gets an outline.</summary>
    public bool IsHighContrast { get; init; }
}
