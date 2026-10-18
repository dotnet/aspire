// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aspire.Tray;

/// <summary>
/// The flyout panel shown by a left click on the status item. Layout, glyphs and interaction
/// live in the shared <see cref="TrayPanel"/>; this part only hosts it in a borderless NSPanel,
/// forwards input, and draws the display list with NSBezierPath and NSAttributedString.
/// VoiceOver sees the panel through MacTrayApplication.PanelAccessibility.cs. A right click,
/// Control-click, or keyboard or VoiceOver activation of the status item still opens the
/// native menu.
/// </summary>
internal sealed partial class MacTrayApplication
{
    // NSEvent types and modifier flags.
    // https://developer.apple.com/documentation/appkit/nsevent/eventtype
    private const long LeftMouseDownEvent = 1;
    private const long ControlKeyMask = 1 << 18;
    private const long ShiftKeyMask = 1 << 17;
    private const long CommandKeyMask = 1 << 20;
    private const double PanelScreenMargin = 8;

    private readonly Dictionary<PanelFontRole, nint> _panelFonts = [];
    private TrayPanel? _panel;
    private nint _panelWindow;
    private nint _panelView;
    private long _panelHiddenAt;
    private bool _panelVisible;

    private static unsafe void RegisterPanelClasses()
    {
        var windowClass = AppKit.AllocateClass(AppKit.Class("NSPanel"), "AspireTrayFlyoutPanel", 0);
        var viewClass = AppKit.AllocateClass(AppKit.Class("NSView"), "AspireTrayFlyoutView", 0);
        if (windowClass == 0 || viewClass == 0)
        {
            throw new InvalidOperationException("Could not allocate the flyout panel classes.");
        }
        // Borderless windows refuse key status by default; the panel needs it for keyboard input.
        // https://developer.apple.com/documentation/appkit/nswindow/canbecomekey
        AddMethod(windowClass, "canBecomeKeyWindow", (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&ReturnYes, "c@:");
        AddMethod(viewClass, "isFlipped", (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&ReturnYes, "c@:");
        AddMethod(viewClass, "acceptsFirstResponder", (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&ReturnYes, "c@:");
        AddMethod(viewClass, "acceptsFirstMouse:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, byte>)&ReturnYesForEvent, "c@:@");
        AddMethod(viewClass, "drawRect:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, AppKit.NativeRect, void>)&OnPanelDraw,
            "v@:{CGRect={CGPoint=dd}{CGSize=dd}}");
        AddMethod(viewClass, "viewDidChangeEffectiveAppearance", (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&OnPanelAppearance, "v@:");
        foreach (var (name, callback) in new (string, nint)[]
        {
            ("mouseMoved:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelMouseMoved),
            ("mouseDragged:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelMouseMoved),
            ("mouseExited:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelMouseExited),
            ("mouseDown:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelMouseDown),
            ("mouseUp:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelMouseUp),
            ("scrollWheel:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelScroll),
            ("keyDown:", (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnPanelKeyDown)
        })
        {
            AddMethod(viewClass, name, callback, "v@:@");
        }
        RegisterPanelAccessibilityClasses(viewClass);
        AppKit.RegisterClass(windowClass);
        AppKit.RegisterClass(viewClass);

        static void AddMethod(nint cls, string selector, nint implementation, string types)
        {
            if (AppKit.AddMethod(cls, AppKit.Selector(selector), implementation, types) == 0)
            {
                throw new InvalidOperationException("Could not register a flyout panel method.");
            }
        }
    }

    private void AttachStatusItemAction()
    {
        var button = AppKit.Get(_statusItem, "button");
        AppKit.Set(button, "setTarget:", _target);
        AppKit.Set(button, "setAction:", AppKit.Selector("statusItemClicked:"));
        // Act on mouse-down, like a menu, for both buttons; the right button opens the menu.
        // https://developer.apple.com/documentation/appkit/nscontrol/sendaction(on:)
        AppKit.SendInt64(button, AppKit.Selector("sendActionOn:"), (1 << 1) | (1 << 3));
    }

    private void OnStatusItemActivated()
    {
        var currentEvent = AppKit.Get(_application, "currentEvent");
        var type = currentEvent == 0 ? 0 : AppKit.GetInt64(currentEvent, AppKit.Selector("type"));
        var modifiers = currentEvent == 0 ? 0 : AppKit.GetInt64(currentEvent, AppKit.Selector("modifierFlags"));
        // Only a plain left mouse-down opens the flyout. Keyboard and VoiceOver activation
        // (AXPress), right clicks and Control-clicks get the native menu, which is accessible.
        if (type != LeftMouseDownEvent || (modifiers & ControlKeyMask) != 0)
        {
            HidePanel();
            ShowNativeMenu();
            return;
        }
        if (_panelVisible || Environment.TickCount64 - _panelHiddenAt < 250)
        {
            // A click on the status item can first take key status away from the panel, which
            // already closed it; that click should not reopen it.
            HidePanel();
            return;
        }
        ShowPanel();
    }

    private void ShowNativeMenu()
    {
        // The standard way to open a status item's menu on demand: attach it, click, detach.
        // performClick: returns when menu tracking ends.
        // https://developer.apple.com/documentation/appkit/nsstatusitem/menu
        var button = AppKit.Get(_statusItem, "button");
        AppKit.Set(_statusItem, "setMenu:", _menu);
        try
        {
            AppKit.Set(button, "performClick:", 0);
        }
        finally
        {
            AppKit.Set(_statusItem, "setMenu:", 0);
        }
    }

    private void ShowPanel()
    {
        if (_openMenus.Count != 0 || _modalDepth != 0 || _quitting)
        {
            return;
        }
        EnsurePanel();
        _controller.PruneMissingPinnedAppHosts();
        _panel!.Update(_controller.State);
        _panel.Reset();
        _panel.Palette = GetPanelPalette();
        _panelVisible = true;
        LayoutPanel();
        AppKit.SendBool(AppKit.Get(_statusItem, "button"), AppKit.Selector("highlight:"), 1);
        AppKit.SendBool(_application, AppKit.Selector("activateIgnoringOtherApps:"), 1);
        AppKit.Set(_panelWindow, "makeKeyAndOrderFront:", 0);
        AppKit.SendReturningBool(_panelWindow, AppKit.Selector("makeFirstResponder:"), _panelView);
    }

    private void HidePanel()
    {
        if (!_panelVisible)
        {
            return;
        }
        _panelVisible = false;
        _panelHiddenAt = Environment.TickCount64;
        AppKit.Set(_panelWindow, "orderOut:", 0);
        if (_statusItem != 0)
        {
            AppKit.SendBool(AppKit.Get(_statusItem, "button"), AppKit.Selector("highlight:"), 0);
        }
    }

    private void EnsurePanel()
    {
        if (_panelWindow != 0)
        {
            return;
        }
        _panel = new TrayPanel(_controller.State, GetPanelPalette());
        var frame = new AppKit.NativeRect(new(0, 0), new(TrayPanel.Width, 200));
        // Borderless (0), buffered backing (2).
        _panelWindow = AppKit.CreateWindow(AppKit.Get(AppKit.Class("AspireTrayFlyoutPanel"), "alloc"),
            AppKit.Selector("initWithContentRect:styleMask:backing:defer:"), frame, 0, 2, 0);
        if (_panelWindow == 0)
        {
            throw new InvalidOperationException("Could not create the flyout panel.");
        }
        AppKit.SendBool(_panelWindow, AppKit.Selector("setReleasedWhenClosed:"), 0);
        AppKit.SendBool(_panelWindow, AppKit.Selector("setOpaque:"), 0);
        AppKit.Set(_panelWindow, "setBackgroundColor:", AppKit.Get(AppKit.Class("NSColor"), "clearColor"));
        AppKit.SendBool(_panelWindow, AppKit.Selector("setHasShadow:"), 1);
        AppKit.SendBool(_panelWindow, AppKit.Selector("setHidesOnDeactivate:"), 0);
        // NSPopUpMenuWindowLevel, the level menus use, so the panel sits above normal windows.
        AppKit.SendInt64(_panelWindow, AppKit.Selector("setLevel:"), 101);
        // Join the active Space and float over full-screen apps, like a menu.
        // https://developer.apple.com/documentation/appkit/nswindow/collectionbehavior-swift.struct
        AppKit.SendInt64(_panelWindow, AppKit.Selector("setCollectionBehavior:"), (1 << 1) | (1 << 3) | (1 << 8));
        AppKit.Set(_panelWindow, "setDelegate:", _target);
        AppKit.Set(_panelWindow, "setAccessibilityLabel:", AppKit.String("Aspire"));

        _panelView = AppKit.SendRect(AppKit.Get(AppKit.Class("AspireTrayFlyoutView"), "alloc"), AppKit.Selector("initWithFrame:"), frame);
        // MouseEnteredAndExited | MouseMoved | ActiveAlways | InVisibleRect: the area follows
        // the view's size, so it never needs updating when the panel height changes.
        // https://developer.apple.com/documentation/appkit/nstrackingarea/options
        var tracking = AppKit.CreateTrackingArea(AppKit.Get(AppKit.Class("NSTrackingArea"), "alloc"),
            AppKit.Selector("initWithRect:options:owner:userInfo:"), default, 0x01 | 0x02 | 0x80 | 0x200, _panelView, 0);
        AppKit.Set(_panelView, "addTrackingArea:", tracking);
        AppKit.Release(tracking);
        AppKit.Set(_panelWindow, "setContentView:", _panelView);
        ConfigurePanelViewAccessibility();
    }

    private void LayoutPanel()
    {
        var button = AppKit.Get(_statusItem, "button");
        var buttonWindow = AppKit.Get(button, "window");
        var anchor = buttonWindow == 0 ? default : AppKit.GetRect(buttonWindow, "frame");
        var screen = buttonWindow == 0 ? 0 : AppKit.Get(buttonWindow, "screen");
        if (screen == 0)
        {
            screen = AppKit.Get(AppKit.Class("NSScreen"), "mainScreen");
        }
        var visible = AppKit.GetRect(screen, "visibleFrame");
        if (buttonWindow == 0)
        {
            anchor = new(new(visible.Origin.X + visible.Size.Width - TrayPanel.Width, visible.Origin.Y + visible.Size.Height), new(0, 0));
        }
        // Screen coordinates have a bottom-left origin. The panel hangs below the menu bar,
        // centered on the status item and kept inside the visible frame.
        var top = Math.Min(anchor.Origin.Y - 4, visible.Origin.Y + visible.Size.Height);
        _panel!.MaxHeight = Math.Min(TrayPanel.DefaultMaxHeight, top - visible.Origin.Y - PanelScreenMargin);
        var height = _panel.Scene.Height;
        var x = anchor.Origin.X + anchor.Size.Width / 2 - TrayPanel.Width / 2;
        x = Math.Clamp(x, visible.Origin.X + PanelScreenMargin,
            Math.Max(visible.Origin.X + PanelScreenMargin, visible.Origin.X + visible.Size.Width - PanelScreenMargin - TrayPanel.Width));
        AppKit.SetFrame(_panelWindow, AppKit.Selector("setFrame:display:"),
            new(new(Math.Round(x), Math.Round(top - height)), new(TrayPanel.Width, height)), 1);
        RedrawPanel();
    }

    private void UpdatePanel(TrayViewState state)
    {
        if (!_panelVisible || _panel is null)
        {
            return;
        }
        if (_panel.Update(state))
        {
            ApplyPanelResponse(PanelResponse.Changed);
        }
    }

    private void RedrawPanel()
    {
        AppKit.SendBool(_panelView, AppKit.Selector("setNeedsDisplay:"), 1);
        // The window shadow follows the drawn alpha (the rounded corners), so recompute it
        // after the content changes shape.
        AppKit.SendVoid(_panelView, AppKit.Selector("displayIfNeeded"));
        AppKit.SendVoid(_panelWindow, AppKit.Selector("invalidateShadow"));
        SyncPanelAccessibility();
    }

    private void ApplyPanelResponse(PanelResponse response)
    {
        if (response.Redraw)
        {
            var frame = AppKit.GetRect(_panelWindow, "frame");
            if (Math.Abs(frame.Size.Height - _panel!.Scene.Height) > 0.5)
            {
                LayoutPanel();
            }
            else
            {
                RedrawPanel();
            }
        }
        if (response.Action is { } action)
        {
            RunPanelAction(action);
        }
    }

    private void RunPanelAction(PanelAction action)
    {
        switch (action.Kind)
        {
            case PanelActionKind.OpenDashboard:
                HidePanel();
                OpenDashboard(action.Host!.Id);
                break;
            case PanelActionKind.Stop:
                // The panel confirmed inline (or confirmation is off): no NSAlert here.
                StopAppHostWithoutConfirmation(action.Host!.Id, action.DontAskAgain);
                break;
            case PanelActionKind.Start:
                RunAction(() => StartFromPanel(action));
                break;
            case PanelActionKind.More:
                ShowRowMenu(action);
                break;
            case PanelActionKind.ClearRecent:
                RunAction(_controller.ClearRecent);
                break;
            case PanelActionKind.RemoveRecent:
                RunAction(() => _controller.RemoveRecent(action.Host!.Id.AppHostPath));
                break;
            case PanelActionKind.Documentation:
                HidePanel();
                RunAction(() => _openDashboard(new Uri("https://aspire.dev")));
                break;
            case PanelActionKind.Settings:
                ApplyPanelResponse(_panel!.ShowSettings(PanelSettings.Read(_startupSettings, _interactiveSmoke, LogStartupReadFailure)));
                break;
            case PanelActionKind.SetLaunchAtSignIn:
                ApplyPanelResponse(_panel!.UpdateSettings(PanelSettings.SetLaunchAtSignIn(_startupSettings, action.Enabled, _interactiveSmoke,
                    static ex => LogFailure("Unable to change launch-at-sign-in settings", ex))));
                if (_settingsWindow != 0)
                {
                    // Keep an open native Settings window in sync with the panel's toggle.
                    ReloadStartupSettings();
                }
                break;
            case PanelActionKind.SetConfirmStop:
                RunAction(() => _controller.SetConfirmStop(action.Enabled));
                break;
            case PanelActionKind.Quit:
                HidePanel();
                Quit();
                break;
            case PanelActionKind.Close:
                HidePanel();
                break;
        }
    }

    private static void LogStartupReadFailure(Exception exception) => LogFailure("Unable to read launch-at-sign-in settings", exception);

    /// <summary>
    /// Starts a pinned or recent AppHost from the panel. A recent entry whose source file is gone
    /// turns into an inline removal prompt instead of the NSAlert the native menu shows.
    /// </summary>
    private void StartFromPanel(PanelAction action)
    {
        var path = action.Host!.Id.AppHostPath;
        if (action.Recent && TrayAppHostPath.IsMissing(path)
            && _controller.State.RecentAppHosts.FirstOrDefault(host => host.Id.AppHostPath == path) is { } recent)
        {
            ApplyPanelResponse(_panel!.ConfirmRemoveMissing(recent));
            return;
        }
        StartAppHost(path);
    }

    private void ShowRowMenu(PanelAction action)
    {
        // Reuse the row's native action submenu (Open In, Finder, Copy Path, Pin) instead of
        // rebuilding it. A copy is popped so the original keeps its place in the status menu.
        var row = (action.Recent ? _recentRows : _rows).FirstOrDefault(row => row.Id == action.Host!.Id)
            ?? _rows.Concat(_recentRows).FirstOrDefault(row => row.Id.AppHostPath == action.Host!.Id.AppHostPath);
        if (row is null)
        {
            return;
        }
        var menu = AppKit.Get(row.Submenu, "copy");
        try
        {
            AppKit.Set(menu, "setDelegate:", _target);
            _menus.Add(menu);
            // The view is flipped, so the anchor's bottom-left is in panel coordinates directly.
            AppKit.PopUpMenu(menu, AppKit.Selector("popUpMenuPositioningItem:atLocation:inView:"),
                0, new(action.Anchor.X, action.Anchor.Bottom + 2), _panelView);
        }
        finally
        {
            AppKit.Set(menu, "setDelegate:", 0);
            _menus.Remove(menu);
            AppKit.Release(menu);
        }
    }

    private TrayPanelPalette GetPanelPalette()
    {
        var appearance = AppKit.Get(_panelView != 0 ? _panelView : _application, "effectiveAppearance");
        var dark = false;
        if (appearance != 0)
        {
            unsafe
            {
                nint* names = stackalloc nint[2] { AppKit.Constant("NSAppearanceNameAqua"), AppKit.Constant("NSAppearanceNameDarkAqua") };
                var array = AppKit.CreateArray(AppKit.Class("NSArray"), AppKit.Selector("arrayWithObjects:count:"), names, 2);
                var match = AppKit.Get(appearance, "bestMatchFromAppearancesWithNames:", array);
                dark = match != 0 && AppKit.Text(match) == AppKit.Text(names[1]);
            }
        }
        var palette = dark ? TrayPanelPalette.Dark : TrayPanelPalette.Light;
        var workspace = AppKit.Get(AppKit.Class("NSWorkspace"), "sharedWorkspace");
        return AppKit.GetBool(workspace, AppKit.Selector("accessibilityDisplayShouldIncreaseContrast")) != 0
            ? palette.WithIncreasedContrast()
            : palette;
    }

    private void DrawPanel()
    {
        if (_panel is null)
        {
            return;
        }
        var context = AppKit.Class("NSGraphicsContext");
        foreach (var command in _panel.Scene.Commands)
        {
            switch (command)
            {
                case FillPathCommand fill:
                    SetColor(fill.Color);
                    AppKit.SendVoid(CreateBezierPath(fill.Path), AppKit.Selector("fill"));
                    break;
                case StrokePathCommand stroke:
                    SetColor(stroke.Color);
                    var path = CreateBezierPath(stroke.Path);
                    AppKit.SetDouble(path, AppKit.Selector("setLineWidth:"), stroke.Width);
                    AppKit.SendInt64(path, AppKit.Selector("setLineCapStyle:"), 1);
                    AppKit.SendInt64(path, AppKit.Selector("setLineJoinStyle:"), 1);
                    AppKit.SendVoid(path, AppKit.Selector("stroke"));
                    break;
                case TextCommand text:
                    DrawPanelText(text);
                    break;
                case LogoCommand logo:
                    // NSCompositingOperationSourceOver (2), drawn upright in the flipped view.
                    AppKit.DrawImageRespectingFlipped(_brandImage,
                        AppKit.Selector("drawInRect:fromRect:operation:fraction:respectFlipped:hints:"),
                        ToNative(logo.Bounds), default, 2, 1, 1, 0);
                    break;
                case PushClipCommand clip:
                    AppKit.SendVoid(context, AppKit.Selector("saveGraphicsState"));
                    AppKit.SetRect(AppKit.Class("NSBezierPath"), AppKit.Selector("clipRect:"), ToNative(clip.Bounds));
                    break;
                case PopClipCommand:
                    AppKit.SendVoid(context, AppKit.Selector("restoreGraphicsState"));
                    break;
            }
        }
    }

    private static void SetColor(PanelColor color)
        => AppKit.SendVoid(CreateColor(color), AppKit.Selector("set"));

    private static nint CreateColor(PanelColor color)
        => AppKit.SendFourDoubles(AppKit.Class("NSColor"), AppKit.Selector("colorWithSRGBRed:green:blue:alpha:"),
            color.R / 255.0, color.G / 255.0, color.B / 255.0, color.Alpha);

    private static nint CreateBezierPath(PanelPath source)
    {
        var path = AppKit.Get(AppKit.Class("NSBezierPath"), "bezierPath");
        var moveTo = AppKit.Selector("moveToPoint:");
        var lineTo = AppKit.Selector("lineToPoint:");
        var curveTo = AppKit.Selector("curveToPoint:controlPoint1:controlPoint2:");
        foreach (var segment in source.Segments)
        {
            switch (segment.Verb)
            {
                case PanelPathVerb.MoveTo:
                    AppKit.SendPoint(path, moveTo, new(segment.X3, segment.Y3));
                    break;
                case PanelPathVerb.LineTo:
                    AppKit.SendPoint(path, lineTo, new(segment.X3, segment.Y3));
                    break;
                case PanelPathVerb.CubicTo:
                    AppKit.SendThreePoints(path, curveTo, new(segment.X3, segment.Y3), new(segment.X1, segment.Y1), new(segment.X2, segment.Y2));
                    break;
                default:
                    AppKit.SendVoid(path, AppKit.Selector("closePath"));
                    break;
            }
        }
        return path;
    }

    private unsafe void DrawPanelText(TextCommand text)
    {
        var font = GetPanelFont(text.Font);
        var style = AppKit.Get(AppKit.Class("NSMutableParagraphStyle"), "new");
        try
        {
            // NSLineBreakMode: byWordWrapping (0), byTruncatingTail (4), byTruncatingMiddle (5).
            AppKit.SendInt64(style, AppKit.Selector("setLineBreakMode:"),
                text.MaxLines > 1 ? 0 : text.Truncation == PanelTruncation.Middle ? 5 : 4);
            AppKit.SendInt64(style, AppKit.Selector("setAlignment:"), GetTextAlignment(text.Alignment));
            nint* keys = stackalloc nint[3]
            {
                AppKit.Constant("NSFontAttributeName"),
                AppKit.Constant("NSForegroundColorAttributeName"),
                AppKit.Constant("NSParagraphStyleAttributeName")
            };
            nint* values = stackalloc nint[3] { font, CreateColor(text.Color), style };
            var attributes = AppKit.CreateDictionary(AppKit.Class("NSDictionary"),
                AppKit.Selector("dictionaryWithObjects:forKeys:count:"), values, keys, 3);
            var value = AppKit.SendTwoPointers(AppKit.Get(AppKit.Class("NSAttributedString"), "alloc"),
                AppKit.Selector("initWithString:attributes:"), AppKit.String(text.Text), attributes);
            try
            {
                // Size the box from the font's line metrics: a single line is centered between
                // ascent and descent, multi-line text starts at the top of its bounds.
                var lineHeight = AppKit.GetDouble(font, AppKit.Selector("ascender")) - AppKit.GetDouble(font, AppKit.Selector("descender"))
                    + AppKit.GetDouble(font, AppKit.Selector("leading"));
                var bounds = text.MaxLines > 1
                    ? new AppKit.NativeRect(new(text.Bounds.X, text.Bounds.Y), new(text.Bounds.Width, Math.Min(text.Bounds.Height, lineHeight * text.MaxLines)))
                    : new AppKit.NativeRect(new(text.Bounds.X, text.Bounds.CenterY - lineHeight / 2), new(text.Bounds.Width, lineHeight));
                // NSStringDrawingUsesLineFragmentOrigin (1) | NSStringDrawingTruncatesLastVisibleLine (32).
                // https://developer.apple.com/documentation/foundation/nsstring/drawingoptions
                AppKit.DrawWithRect(value, AppKit.Selector("drawWithRect:options:context:"), bounds, 1 | 32, 0);
            }
            finally
            {
                AppKit.Release(value);
            }
        }
        finally
        {
            AppKit.Release(style);
        }
    }

    private static long GetTextAlignment(PanelTextAlignment alignment)
    {
        // NSTextAlignment uses the iOS values (center 1, right 2) on Apple silicon and the
        // historical macOS values (right 1, center 2) on Intel.
        // https://developer.apple.com/documentation/appkit/nstextalignment
        var intel = RuntimeInformation.ProcessArchitecture == Architecture.X64;
        return alignment switch
        {
            PanelTextAlignment.Center => intel ? 2 : 1,
            PanelTextAlignment.Trailing => intel ? 1 : 2,
            _ => 0
        };
    }

    private nint GetPanelFont(PanelFontRole role)
    {
        if (!_panelFonts.TryGetValue(role, out var font))
        {
            var weight = PanelFonts.IsSemibold(role)
                ? AppKit.DoubleConstant("NSFontWeightSemibold")
                : AppKit.DoubleConstant("NSFontWeightRegular");
            font = AppKit.Get(AppKit.SendTwoDoubles(AppKit.Class("NSFont"), AppKit.Selector("systemFontOfSize:weight:"),
                PanelFonts.Size(role), weight), "retain");
            _panelFonts.Add(role, font);
        }
        return font;
    }

    private static AppKit.NativeRect ToNative(PanelRect rect) => new(new(rect.X, rect.Y), new(rect.Width, rect.Height));

    private (double X, double Y) GetPanelPoint(nint nativeEvent)
    {
        var point = AppKit.ConvertPoint(_panelView, AppKit.Selector("convertPoint:fromView:"),
            AppKit.GetPoint(nativeEvent, AppKit.Selector("locationInWindow")), 0);
        return (point.X, point.Y);
    }

    private void HandlePanelEvent(string kind, nint nativeEvent)
    {
        if (_panel is null || !_panelVisible)
        {
            return;
        }
        var response = PanelResponse.None;
        switch (kind)
        {
            case "moved":
                var (mx, my) = GetPanelPoint(nativeEvent);
                response = _panel.PointerMoved(mx, my);
                break;
            case "exited":
                response = _panel.PointerLeft();
                break;
            case "down":
                var (dx, dy) = GetPanelPoint(nativeEvent);
                response = _panel.PointerDown(dx, dy);
                break;
            case "up":
                var (ux, uy) = GetPanelPoint(nativeEvent);
                response = _panel.PointerUp(ux, uy);
                break;
            case "scroll":
                var delta = AppKit.GetDouble(nativeEvent, AppKit.Selector("scrollingDeltaY"));
                // Mouse wheels report lines rather than points; scale a line to a short step.
                if (AppKit.GetBool(nativeEvent, AppKit.Selector("hasPreciseScrollingDeltas")) == 0)
                {
                    delta *= 16;
                }
                response = _panel.Scroll(delta);
                break;
            case "key":
                response = HandlePanelKey(nativeEvent);
                break;
        }
        ApplyPanelResponse(response);
    }

    private PanelResponse HandlePanelKey(nint nativeEvent)
    {
        // keyCode is an unsigned short; ignore any upper register bits.
        var keyCode = AppKit.GetInt64(nativeEvent, AppKit.Selector("keyCode")) & 0xFFFF;
        var modifiers = AppKit.GetInt64(nativeEvent, AppKit.Selector("modifierFlags"));
        if ((modifiers & CommandKeyMask) != 0)
        {
            // Command-Q quits and Command-W closes, as in any other window.
            return keyCode switch
            {
                12 => new(false, new PanelAction(PanelActionKind.Quit)),
                13 => new(false, new PanelAction(PanelActionKind.Close)),
                _ => PanelResponse.None
            };
        }
        // Virtual key codes from HIToolbox Events.h (kVK_*).
        PanelKey? key = keyCode switch
        {
            126 => PanelKey.Up,
            125 => PanelKey.Down,
            48 => (modifiers & ShiftKeyMask) != 0 ? PanelKey.ShiftTab : PanelKey.Tab,
            36 or 76 => PanelKey.Enter,
            49 => PanelKey.Space,
            53 => PanelKey.Escape,
            115 => PanelKey.Home,
            119 => PanelKey.End,
            _ => null
        };
        return key is { } value ? _panel!.Key(value) : PanelResponse.None;
    }

    /// <summary>
    /// Opens the flyout through the production path and checks the native window against the
    /// shared scene. When <paramref name="captureDirectory"/> is set, also writes PNGs rendered by
    /// AppKit itself (light, dark, and dark with hover and keyboard focus) for visual review;
    /// caching the view's display needs no screen-recording permission.
    /// </summary>
    internal string VerifyPanelForSmoke(string? captureDirectory)
    {
        VerifyUIThread();
        var button = AppKit.Get(_statusItem, "button");
        Require(AppKit.Get(_statusItem, "menu") == 0, "The status item must not keep its menu attached; left click opens the panel.");
        Require(AppKit.Get(button, "action") == AppKit.Selector("statusItemClicked:") && AppKit.Get(button, "target") == _target,
            "The status button must route clicks to the panel/menu selector.");
        ShowPanel();
        try
        {
            Require(_panelVisible && AppKit.GetBool(_panelWindow, AppKit.Selector("isVisible")) != 0, "The flyout panel did not open.");
            var frame = AppKit.GetRect(_panelWindow, "frame");
            var scene = _panel!.Scene;
            Require(frame.Size.Width == TrayPanel.Width && Math.Abs(frame.Size.Height - scene.Height) < 0.5,
                $"The panel window ({frame.Size.Width}x{frame.Size.Height}) does not match the scene ({scene.Width}x{scene.Height}).");
            Require(AppKit.GetBool(_panelView, AppKit.Selector("isFlipped")) != 0, "The panel view must use top-left coordinates.");
            Require(AppKit.GetBool(_panelWindow, AppKit.Selector("canBecomeKeyWindow")) != 0, "The borderless panel must accept key status.");
            Require(scene.Targets.Count(target => target.Kind == PanelTargetKind.Row) == _controller.State.AppHosts.Count
                + (_panel.IsRecentExpanded ? _controller.State.RecentAppHosts.Count : 0), "The panel must list every AppHost.");
            VerifyPanelAccessibilityForSmoke("the list");
            var captures = new List<string>();
            if (captureDirectory is not null)
            {
                Directory.CreateDirectory(captureDirectory);
                foreach (var (name, appearance) in new[] { ("light", "NSAppearanceNameAqua"), ("dark", "NSAppearanceNameDarkAqua") })
                {
                    AppKit.Set(_panelView, "setAppearance:", AppKit.Get(AppKit.Class("NSAppearance"), "appearanceNamed:", AppKit.Constant(appearance)));
                    _panel.Palette = GetPanelPalette();
                    captures.Add(CapturePanel(Path.Combine(captureDirectory, $"panel-{name}.png")));
                }
                var row = _panel.Scene.Targets.Where(target => target.Kind == PanelTargetKind.Row).Skip(1).FirstOrDefault();
                if (row is not null)
                {
                    _panel.Key(PanelKey.Down);
                    _panel.PointerMoved(row.HitBounds.X + 40, row.HitBounds.CenterY);
                    captures.Add(CapturePanel(Path.Combine(captureDirectory, "panel-dark-interactive.png")));
                }
                AppKit.Set(_panelView, "setAppearance:", 0);
                _panel.Palette = GetPanelPalette();
            }

            // Stop asks inline: the row turns into a confirmation strip and no NSAlert opens.
            // Escape cancels it and keeps the panel open.
            var modalDepth = _modalDepth;
            if (_controller.ConfirmStop && _panel.Scene.Targets.FirstOrDefault(target => target is { Kind: PanelTargetKind.Stop, Enabled: true }) is { } stop)
            {
                ClickPanelForSmoke(stop);
                Require(_panel.Confirmation == PanelConfirmKind.Stop && _modalDepth == modalDepth && _panelVisible,
                    "Stop must ask inline in the panel, without a modal alert.");
                Require(_panel.FocusedTarget?.Kind == PanelTargetKind.ConfirmCancel, "The inline confirmation must focus Cancel.");
                RequireWindowMatchesScene();
                VerifyPanelAccessibilityForSmoke("the Stop confirmation");
                if (captureDirectory is not null)
                {
                    captures.Add(CapturePanel(Path.Combine(captureDirectory, "panel-confirm-stop.png")));
                }
                ApplyPanelResponse(_panel.Key(PanelKey.Escape));
                Require(_panel.Confirmation is null && _panelVisible, "Escape must cancel the inline confirmation and keep the panel open.");
            }

            // The gear opens Settings inside the panel; its toggle round-trips through the
            // startup settings (an isolated in-memory store during smoke).
            ClickPanelForSmoke(_panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.Settings));
            Require(_panel.IsSettingsOpen && _panelVisible
                && (_settingsWindow == 0 || AppKit.GetBool(_settingsWindow, AppKit.Selector("isVisible")) == 0),
                "The gear must open the Settings view inside the panel, not the Settings window.");
            RequireWindowMatchesScene();
            VerifyPanelAccessibilityForSmoke("the Settings view");
            var launch = _panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn);
            if (launch.Enabled)
            {
                var before = _startupSettings.Read().Enabled;
                // Press the switch the way VoiceOver does (AXPress), then check its new value.
                OnPanelElementPressed(PanelAccessibleKey.For(launch.Key));
                Require(_startupSettings.Read().Enabled != before, "Pressing the Settings switch must change launch at sign-in.");
                VerifyPanelAccessibilityForSmoke("the Settings view after a press");
                ClickPanelForSmoke(_panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn));
                Require(_startupSettings.Read().Enabled == before, "The Settings view toggle must change launch at sign-in back.");
            }
            if (captureDirectory is not null)
            {
                foreach (var (name, appearance) in new[] { ("light", "NSAppearanceNameAqua"), ("dark", "NSAppearanceNameDarkAqua") })
                {
                    AppKit.Set(_panelView, "setAppearance:", AppKit.Get(AppKit.Class("NSAppearance"), "appearanceNamed:", AppKit.Constant(appearance)));
                    _panel.Palette = GetPanelPalette();
                    captures.Add(CapturePanel(Path.Combine(captureDirectory, $"panel-settings-{name}.png")));
                }
                AppKit.Set(_panelView, "setAppearance:", 0);
                _panel.Palette = GetPanelPalette();
            }
            ApplyPanelResponse(_panel.Key(PanelKey.Escape));
            Require(!_panel.IsSettingsOpen && _panelVisible, "Escape must leave the Settings view and keep the panel open.");
            RequireWindowMatchesScene();

            var escape = _panel.Key(PanelKey.Escape);
            ApplyPanelResponse(escape);
            Require(!_panelVisible && AppKit.GetBool(_panelWindow, AppKit.Selector("isVisible")) == 0, "Escape must close the panel.");
            return captures.Count == 0 ? "panel verified" : $"panel verified; captures: {string.Join(", ", captures)}";
        }
        finally
        {
            HidePanel();
        }

        void RequireWindowMatchesScene()
        {
            var height = AppKit.GetRect(_panelWindow, "frame").Size.Height;
            Require(Math.Abs(height - _panel!.Scene.Height) < 0.5, $"The panel window ({height}) must resize to the scene ({_panel.Scene.Height}).");
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    private void ClickPanelForSmoke(PanelHitTarget target)
    {
        var (x, y) = (target.HitBounds.CenterX, target.HitBounds.CenterY);
        ApplyPanelResponse(_panel!.PointerMoved(x, y));
        ApplyPanelResponse(_panel.PointerDown(x, y));
        ApplyPanelResponse(_panel.PointerUp(x, y));
    }

    private string CapturePanel(string path)
    {
        LayoutPanel();
        CaptureView(_panelView, path);
        return path;
    }

    private void DisposePanel()
    {
        if (_panelWindow != 0)
        {
            AppKit.Set(_panelWindow, "setDelegate:", 0);
            AppKit.Set(_panelWindow, "orderOut:", 0);
            DisposePanelAccessibility();
            AppKit.Release(_panelView);
            AppKit.Release(_panelWindow);
            _panelWindow = 0;
            _panelView = 0;
        }
        foreach (var font in _panelFonts.Values)
        {
            AppKit.Release(font);
        }
        _panelFonts.Clear();
        _panelVisible = false;
    }

    private static void RouteView(nint view, Action<MacTrayApplication> callback)
    {
        var application = Volatile.Read(ref s_callbackRoot);
        if (application is null || application._disposed || view != application._panelView)
        {
            return;
        }
        Route(application._target, 0, (app, _) => callback(app));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte ReturnYes(nint self, nint selector) => 1;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte ReturnYesForEvent(nint self, nint selector, nint nativeEvent) => 1;

    private static unsafe void AddPanelCallbacks(nint callbackClass)
    {
        AddCallback(callbackClass, "statusItemClicked:", &OnStatusItemClicked);
        AddCallback(callbackClass, "windowDidResignKey:", &OnPanelResignKey);
        AddCallback(callbackClass, "panelAccessibilityPress:", &OnPanelAccessibilityPress);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnStatusItemClicked(nint self, nint selector, nint sender)
        => Route(self, sender, static (app, _) => app.OnStatusItemActivated());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelResignKey(nint self, nint selector, nint notification)
        => Route(self, notification, static (app, _) => app.HidePanel());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelDraw(nint self, nint selector, AppKit.NativeRect dirty)
        => RouteView(self, static app => app.DrawPanel());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelAppearance(nint self, nint selector)
        => RouteView(self, static app =>
        {
            app._panel!.Palette = app.GetPanelPalette();
            app.RedrawPanel();
        });

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelMouseMoved(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("moved", nativeEvent));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelMouseExited(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("exited", nativeEvent));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelMouseDown(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("down", nativeEvent));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelMouseUp(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("up", nativeEvent));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelScroll(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("scroll", nativeEvent));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPanelKeyDown(nint self, nint selector, nint nativeEvent)
        => RouteView(self, app => app.HandlePanelEvent("key", nativeEvent));
}
