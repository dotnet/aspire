// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Tray;

/// <summary>
/// The text styles of the panel. Renderers map each role to the system UI font at the
/// same size in DIPs (SF on macOS, Segoe UI Variable Text on Windows) without embedding a font.
/// </summary>
internal enum PanelFontRole
{
    /// <summary>15 semibold: the panel title.</summary>
    Title,
    /// <summary>13 semibold: AppHost names and the empty-state title.</summary>
    Name,
    /// <summary>12 regular: status lines, subtitles, banner and button labels.</summary>
    Body,
    /// <summary>11 semibold: uppercase section headers.</summary>
    Section
}

internal static class PanelFonts
{
    public static double Size(PanelFontRole role) => role switch
    {
        PanelFontRole.Title => 15,
        PanelFontRole.Name => 13,
        PanelFontRole.Section => 11,
        _ => 12
    };

    public static bool IsSemibold(PanelFontRole role) => role is not PanelFontRole.Body;
}

internal enum PanelTextAlignment
{
    Leading,
    Center,
    Trailing
}

internal enum PanelTruncation
{
    End,
    Middle
}

/// <summary>One drawing instruction. Commands are drawn in order, painter's algorithm.</summary>
internal abstract record PanelCommand;

internal sealed record FillPathCommand(PanelPath Path, PanelColor Color) : PanelCommand;

/// <summary>Strokes a path with round caps and round joins.</summary>
internal sealed record StrokePathCommand(PanelPath Path, PanelColor Color, double Width) : PanelCommand;

/// <summary>
/// Draws text inside fixed bounds. The layout never measures text: a renderer truncates with
/// an ellipsis to fit. Single-line text is centered vertically from the font's ascent and
/// descent; multi-line text wraps at word boundaries and is top-aligned.
/// </summary>
internal sealed record TextCommand(
    PanelRect Bounds,
    string Text,
    PanelFontRole Font,
    PanelColor Color,
    PanelTextAlignment Alignment = PanelTextAlignment.Leading,
    int MaxLines = 1,
    PanelTruncation Truncation = PanelTruncation.End) : PanelCommand;

/// <summary>Draws the full-color Aspire logo scaled into the bounds.</summary>
internal sealed record LogoCommand(PanelRect Bounds) : PanelCommand;

/// <summary>Intersects the clip with a rectangle until the matching <see cref="PopClipCommand"/>.</summary>
internal sealed record PushClipCommand(PanelRect Bounds) : PanelCommand;

internal sealed record PopClipCommand : PanelCommand;

internal enum PanelTargetKind
{
    Row,
    Dashboard,
    Stop,
    Start,
    More,
    ToggleRecent,
    ClearRecent,
    Documentation,
    Settings,
    Quit
}

internal enum PanelSection
{
    None,
    Running,
    Pinned,
    Recent
}

/// <summary>
/// Identifies a hit target across scene rebuilds, so hover, press and focus survive a
/// refresh even when rows move. The same AppHost can be listed only once per section.
/// </summary>
internal readonly record struct PanelTargetKey(PanelTargetKind Kind, PanelSection Section, AppHostId? Host);

/// <summary>
/// An interactive region. <see cref="Bounds"/> is where it is drawn and <see cref="HitBounds"/>
/// is the part that is visible through the list clip, which is what pointer input tests.
/// </summary>
internal sealed record PanelHitTarget(
    PanelTargetKey Key,
    PanelRect Bounds,
    PanelRect HitBounds,
    string AccessibleName,
    bool Enabled,
    AppHostMenuItem? Host)
{
    public PanelTargetKind Kind => Key.Kind;
}

/// <summary>The output of one layout pass: the panel size, what to draw, and where to click.</summary>
internal sealed record TrayPanelScene(
    double Width,
    double Height,
    IReadOnlyList<PanelCommand> Commands,
    IReadOnlyList<PanelHitTarget> Targets);

internal enum PanelActionKind
{
    OpenDashboard,
    Stop,
    Start,
    More,
    ClearRecent,
    ToggleRecent,
    Documentation,
    Settings,
    Quit,
    Close
}

/// <summary>
/// What the platform host should do in response to input. <see cref="Anchor"/> is the
/// rectangle in panel coordinates a popup menu should be placed against.
/// </summary>
internal sealed record PanelAction(PanelActionKind Kind, AppHostMenuItem? Host = null, PanelRect Anchor = default)
{
    /// <summary>True when the action is for an entry of the recent list rather than a current AppHost.</summary>
    public bool Recent { get; init; }
}

/// <summary>The result of an input event: whether the panel must be redrawn, and an action to run.</summary>
internal readonly record struct PanelResponse(bool Redraw, PanelAction? Action = null)
{
    public static PanelResponse None => default;
    public static PanelResponse Changed => new(true);
}

internal enum PanelKey
{
    Up,
    Down,
    Tab,
    ShiftTab,
    Enter,
    Space,
    Escape,
    Home,
    End
}
