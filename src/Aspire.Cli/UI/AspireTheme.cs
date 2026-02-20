// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Theming;

namespace Aspire.Cli.UI;

/// <summary>
/// Defines the Aspire color theme derived from the Aspire brand palette.
/// </summary>
internal static class AspireTheme
{
    // Brand colors from the Aspire logo SVG
    private static readonly Hex1bColor s_purple = Hex1bColor.FromRgb(81, 43, 212);      // #512BD4 — primary
    private static readonly Hex1bColor s_purpleMedium = Hex1bColor.FromRgb(116, 85, 221); // #7455DD
    private static readonly Hex1bColor s_purpleLight = Hex1bColor.FromRgb(151, 128, 229); // #9780E5
    private static readonly Hex1bColor s_purpleFaint = Hex1bColor.FromRgb(185, 170, 238); // #B9AAEE
    private static readonly Hex1bColor s_lavender = Hex1bColor.FromRgb(220, 213, 246);    // #DCD5F6

    // Dark surfaces
    private static readonly Hex1bColor s_bgDark = Hex1bColor.FromRgb(13, 17, 23);       // #0D1117
    private static readonly Hex1bColor s_bgSurface = Hex1bColor.FromRgb(22, 27, 34);    // #161B22
    private static readonly Hex1bColor s_bgElevated = Hex1bColor.FromRgb(33, 38, 45);   // #21262D
    private static readonly Hex1bColor s_border = Hex1bColor.FromRgb(48, 54, 61);       // #30363D

    // Text
    private static readonly Hex1bColor s_textPrimary = Hex1bColor.FromRgb(230, 237, 243);  // #E6EDF3
    private static readonly Hex1bColor s_textMuted = Hex1bColor.FromRgb(139, 148, 158);    // #8B949E

    /// <summary>
    /// Applies the Aspire color theme to the given base theme.
    /// </summary>
    public static Hex1bTheme Apply(Hex1bTheme theme)
    {
        return theme
            // Global
            .Set(GlobalTheme.BackgroundColor, s_bgDark)
            .Set(GlobalTheme.ForegroundColor, s_textPrimary)

            // Table
            // Border
            .Set(BorderTheme.BorderColor, s_border)
            .Set(BorderTheme.TitleColor, s_purpleLight)

            // TabPanel
            .Set(TabPanelTheme.ContentBackgroundColor, s_bgDark)
            .Set(TabPanelTheme.ContentForegroundColor, s_textPrimary)

            // TabBar
            .Set(TabBarTheme.BackgroundColor, s_bgSurface)
            .Set(TabBarTheme.ForegroundColor, s_textMuted)
            .Set(TabBarTheme.SelectedBackgroundColor, s_bgDark)
            .Set(TabBarTheme.SelectedForegroundColor, s_lavender)

            // InfoBar
            .Set(InfoBarTheme.BackgroundColor, s_purpleMedium)
            .Set(InfoBarTheme.ForegroundColor, Hex1bColor.FromRgb(255, 255, 255))

            // Button
            .Set(ButtonTheme.ForegroundColor, s_textPrimary)
            .Set(ButtonTheme.BackgroundColor, s_bgSurface)
            .Set(ButtonTheme.FocusedForegroundColor, s_lavender)
            .Set(ButtonTheme.FocusedBackgroundColor, s_purple)
            .Set(ButtonTheme.HoveredForegroundColor, s_purpleFaint)
            .Set(ButtonTheme.HoveredBackgroundColor, s_bgElevated)

            // List (AppHost drawer)
            .Set(ListTheme.BackgroundColor, s_bgSurface)
            .Set(ListTheme.ForegroundColor, s_textPrimary)
            .Set(ListTheme.SelectedBackgroundColor, s_purple)
            .Set(ListTheme.SelectedForegroundColor, s_lavender)
            .Set(ListTheme.HoveredBackgroundColor, s_bgElevated)
            .Set(ListTheme.HoveredForegroundColor, s_purpleFaint)

            // Separator
            .Set(SeparatorTheme.Color, s_border)

            // Splitter
            .Set(SplitterTheme.DividerColor, s_purple)
            .Set(SplitterTheme.FocusedDividerColor, s_purple)
            .Set(SplitterTheme.ThumbColor, s_purple)

            // Notification card
            .Set(NotificationCardTheme.BackgroundColor, s_bgElevated)
            .Set(NotificationCardTheme.TitleColor, s_purpleLight)
            .Set(NotificationCardTheme.BodyColor, s_textPrimary)
            .Set(NotificationCardTheme.ProgressBarColor, s_purple);
    }

    /// <summary>
    /// Applies the half-block border theme used for both tiles and the content header.
    /// </summary>
    private static Hex1bTheme ApplyHalfBlockBorder(Hex1bTheme theme, Hex1bColor fillColor)
    {
        return theme
            .Set(BorderTheme.BorderColor, fillColor)
            .Set(BorderTheme.TopLine, "▄")
            .Set(BorderTheme.BottomLine, "▀")
            .Set(BorderTheme.LeftLine, "█")
            .Set(BorderTheme.RightLine, "█")
            .Set(BorderTheme.TopLeftCorner, "▄")
            .Set(BorderTheme.TopRightCorner, "▄")
            .Set(BorderTheme.BottomLeftCorner, "▀")
            .Set(BorderTheme.BottomRightCorner, "▀");
    }

    /// <summary>
    /// Applies the theme for the content area header border.
    /// </summary>
    public static Hex1bTheme ApplyContentHeaderBorder(Hex1bTheme theme)
    {
        return ApplyHalfBlockBorder(theme, s_purple);
    }

    /// <summary>
    /// Applies the inner theme for the content area header.
    /// </summary>
    public static Hex1bTheme ApplyContentHeaderInner(Hex1bTheme theme)
    {
        return theme
            .Set(GlobalTheme.BackgroundColor, s_purple)
            .Set(GlobalTheme.ForegroundColor, Hex1bColor.FromRgb(255, 255, 255))
            .Set(HyperlinkTheme.ForegroundColor, s_lavender)
            .Set(HyperlinkTheme.FocusedForegroundColor, s_lavender)
            .Set(HyperlinkTheme.HoveredForegroundColor, Hex1bColor.FromRgb(255, 255, 255));
    }

    /// <summary>
    /// Applies the AppHost tile theme for the left-pane tiles.
    /// </summary>
    public static Hex1bTheme ApplyAppHostTile(Hex1bTheme theme)
    {
        return ApplyHalfBlockBorder(theme, s_purple);
    }

    /// <summary>
    /// Applies the focused/hovered AppHost tile theme with a lighter background.
    /// </summary>
    public static Hex1bTheme ApplyAppHostTileFocused(Hex1bTheme theme)
    {
        return ApplyHalfBlockBorder(theme, s_purpleMedium);
    }

    /// <summary>
    /// Applies the inner fill color for tile content.
    /// </summary>
    public static Hex1bTheme ApplyAppHostTileInner(Hex1bTheme theme)
    {
        return theme.Set(GlobalTheme.BackgroundColor, s_purple);
    }

    /// <summary>
    /// Applies the inner fill color for focused tile content.
    /// </summary>
    public static Hex1bTheme ApplyAppHostTileInnerFocused(Hex1bTheme theme)
    {
        return theme.Set(GlobalTheme.BackgroundColor, s_purpleMedium);
    }
}
