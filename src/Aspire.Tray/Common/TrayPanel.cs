// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Tray;

/// <summary>
/// The flyout panel shown by a left click on the tray icon. It owns the layout and every bit
/// of interaction state (hover, press, keyboard focus, scrolling, the collapsed recent list)
/// and turns <see cref="TrayViewState"/> into a <see cref="TrayPanelScene"/>. The macOS and
/// Windows hosts only forward input, draw the display list, and run the returned actions,
/// so both platforms look and behave the same.
/// </summary>
/// <remarks>
/// Not thread-safe: hosts call it from their UI thread only.
/// </remarks>
internal sealed class TrayPanel
{
    public const double Width = 360;
    public const double CornerRadius = 8;
    public const double HeaderHeight = 60;
    public const double FooterHeight = 44;
    public const double RowHeight = 52;
    public const double SectionHeaderHeight = 28;
    public const double EmptyStateHeight = 132;
    public const double ListTopPadding = 4;
    public const double ListBottomPadding = 8;
    public const double ButtonSize = 28;
    public const double DefaultMaxHeight = 600;

    // Trailing row buttons are laid out from the right edge of the row's content area.
    private const double RowInset = 8;
    private const double RowContentRight = Width - RowInset - 8;
    private const double ButtonGap = 2;
    private const double TextLeft = 52;

    // The banner estimates whether its text needs a second line from an average glyph width
    // for 12 px system UI text, rather than measuring, so both platforms lay it out the same.
    private const double AverageBodyCharacterWidth = 6.4;

    private TrayViewState _state;
    private TrayViewState? _pendingState;
    private TrayPanelPalette _palette;
    private TrayPanelScene? _scene;
    private double _maxHeight = DefaultMaxHeight;
    private double _scroll;
    private double _contentHeight;
    private PanelRect _viewport;
    private (double X, double Y)? _pointer;
    private PanelTargetKey? _hover;
    private PanelTargetKey? _pressed;
    private PanelTargetKey? _focus;
    private bool _focusVisible;
    private bool? _recentExpanded;

    public TrayPanel(TrayViewState state, TrayPanelPalette palette)
    {
        _state = state;
        _palette = palette;
    }

    public TrayViewState State => _state;

    public TrayPanelPalette Palette
    {
        get => _palette;
        set
        {
            if (_palette != value)
            {
                _palette = value;
                _scene = null;
            }
        }
    }

    /// <summary>The tallest the panel may be, usually the work area height minus a margin.</summary>
    public double MaxHeight
    {
        get => _maxHeight;
        set
        {
            value = Math.Max(HeaderHeight + FooterHeight + RowHeight, value);
            if (_maxHeight != value)
            {
                _maxHeight = value;
                _scene = null;
            }
        }
    }

    public TrayPanelScene Scene => _scene ??= Build();

    /// <summary>The scrolled list area in panel coordinates.</summary>
    public PanelRect Viewport
    {
        get
        {
            _ = Scene;
            return _viewport;
        }
    }

    public double ScrollOffset
    {
        get
        {
            _ = Scene;
            return _scroll;
        }
    }

    public PanelTargetKey? FocusedTarget => _focus;

    public bool IsRecentExpanded => _recentExpanded ?? _state.AppHosts.Count == 0;

    /// <summary>
    /// Replaces the state. While a press is in progress the update is deferred until release,
    /// so the row under the pointer cannot move between press and release.
    /// </summary>
    public bool Update(TrayViewState state)
    {
        if (_pressed is not null)
        {
            _pendingState = state;
            return false;
        }
        _state = state;
        _scene = null;
        return true;
    }

    /// <summary>Clears transient interaction state when the panel is shown again.</summary>
    public void Reset()
    {
        if (_pendingState is not null)
        {
            _state = _pendingState;
            _pendingState = null;
        }
        _pointer = null;
        _hover = null;
        _pressed = null;
        _focus = null;
        _focusVisible = false;
        _scroll = 0;
        _scene = null;
    }

    public PanelHitTarget? HitTest(double x, double y)
    {
        var targets = Scene.Targets;
        // Later targets sit on top: row buttons are added after their row.
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i].HitBounds.Contains(x, y))
            {
                return targets[i];
            }
        }
        return null;
    }

    public PanelResponse PointerMoved(double x, double y)
    {
        _pointer = (x, y);
        return SetHover(HitTest(x, y)?.Key);
    }

    public PanelResponse PointerLeft()
    {
        _pointer = null;
        return SetHover(null);
    }

    public PanelResponse PointerDown(double x, double y)
    {
        _pointer = (x, y);
        var target = HitTest(x, y);
        _hover = target?.Key;
        _pressed = target is { Enabled: true } ? target.Key : null;
        _focusVisible = false;
        _scene = null;
        return PanelResponse.Changed;
    }

    public PanelResponse PointerUp(double x, double y)
    {
        _pointer = (x, y);
        var target = HitTest(x, y);
        var action = _pressed is { } pressed && target is { Enabled: true } && target.Key == pressed ? Activate(target) : null;
        _pressed = null;
        if (_pendingState is not null)
        {
            _state = _pendingState;
            _pendingState = null;
        }
        _scene = null;
        // Hit test again: a deferred state update or a toggled section may have moved rows.
        _hover = HitTest(x, y)?.Key;
        _scene = null;
        return new(true, action);
    }

    /// <summary>
    /// Scrolls the list by <paramref name="deltaY"/> DIPs. A positive delta reveals content above,
    /// matching both NSEvent.scrollingDeltaY and a positive WM_MOUSEWHEEL delta (wheel pushed away).
    /// </summary>
    public PanelResponse Scroll(double deltaY)
    {
        var before = ScrollOffset;
        _scroll = Math.Clamp(before - deltaY, 0, Math.Max(0, _contentHeight - _viewport.Height));
        if (_scroll == before)
        {
            return PanelResponse.None;
        }
        _scene = null;
        if (_pointer is { } pointer)
        {
            _hover = HitTest(pointer.X, pointer.Y)?.Key;
            _scene = null;
        }
        return PanelResponse.Changed;
    }

    public PanelResponse Key(PanelKey key)
    {
        switch (key)
        {
            case PanelKey.Escape:
                return new(false, new PanelAction(PanelActionKind.Close));
            case PanelKey.Enter or PanelKey.Space:
                var focused = Scene.Targets.FirstOrDefault(target => target.Key == _focus);
                if (focused is not { Enabled: true })
                {
                    return PanelResponse.None;
                }
                var action = Activate(focused);
                _scene = null;
                return new(true, action);
            case PanelKey.Tab or PanelKey.ShiftTab:
                return MoveFocus(Scene.Targets.Where(target => target.Enabled).ToList(), key == PanelKey.Tab ? 1 : -1, wrap: true);
            default:
                var primary = Scene.Targets.Where(target => target.Enabled && IsPrimary(target.Kind)).ToList();
                return key switch
                {
                    PanelKey.Home => FocusAt(primary, 0),
                    PanelKey.End => FocusAt(primary, primary.Count - 1),
                    _ => MoveFocus(primary, key == PanelKey.Down ? 1 : -1, wrap: false)
                };
        }
    }

    private static bool IsPrimary(PanelTargetKind kind)
        => kind is PanelTargetKind.Row or PanelTargetKind.ToggleRecent or PanelTargetKind.Documentation
            or PanelTargetKind.Settings or PanelTargetKind.Quit;

    private PanelResponse MoveFocus(List<PanelHitTarget> order, int step, bool wrap)
    {
        if (order.Count == 0)
        {
            return PanelResponse.None;
        }
        var index = order.FindIndex(target => target.Key == _focus);
        if (index < 0 && _focus is { Host: not null } current)
        {
            // Up and Down from an inline button continue from that button's row.
            index = order.FindIndex(target => target.Kind == PanelTargetKind.Row && target.Key.Host == current.Host
                && target.Key.Section == current.Section);
        }
        var next = index < 0
            ? (step > 0 ? 0 : order.Count - 1)
            : wrap ? (index + step + order.Count) % order.Count : Math.Clamp(index + step, 0, order.Count - 1);
        return FocusAt(order, next);
    }

    private PanelResponse FocusAt(List<PanelHitTarget> order, int index)
    {
        if (index < 0 || index >= order.Count)
        {
            return PanelResponse.None;
        }
        var target = order[index];
        _focus = target.Key;
        _focusVisible = true;
        if (IsFooter(target.Kind))
        {
            // Footer buttons never scroll.
        }
        else if (target.Bounds.Y < _viewport.Y)
        {
            _scroll -= _viewport.Y - target.Bounds.Y;
        }
        else if (target.Bounds.Bottom > _viewport.Bottom)
        {
            _scroll += target.Bounds.Bottom - _viewport.Bottom;
        }
        _scene = null;
        return PanelResponse.Changed;
    }

    private PanelResponse SetHover(PanelTargetKey? key)
    {
        if (_hover == key)
        {
            return PanelResponse.None;
        }
        _hover = key;
        _focusVisible = false;
        _scene = null;
        return PanelResponse.Changed;
    }

    private PanelAction? Activate(PanelHitTarget target)
    {
        var host = target.Host;
        var recent = target.Key.Section == PanelSection.Recent;
        switch (target.Kind)
        {
            case PanelTargetKind.Row when host is { CanOpenDashboard: true }:
            case PanelTargetKind.Dashboard:
                return new(PanelActionKind.OpenDashboard, host, target.Bounds) { Recent = recent };
            case PanelTargetKind.Row:
                // Rows without a dashboard open the same menu as their "More" button.
                var more = Scene.Targets.FirstOrDefault(other => other.Key == target.Key with { Kind = PanelTargetKind.More });
                return new(PanelActionKind.More, host, more?.Bounds ?? target.Bounds) { Recent = recent };
            case PanelTargetKind.Stop:
                return new(PanelActionKind.Stop, host, target.Bounds) { Recent = recent };
            case PanelTargetKind.Start:
                return new(PanelActionKind.Start, host, target.Bounds) { Recent = recent };
            case PanelTargetKind.More:
                return new(PanelActionKind.More, host, target.Bounds) { Recent = recent };
            case PanelTargetKind.ToggleRecent:
                _recentExpanded = !IsRecentExpanded;
                _scene = null;
                return new(PanelActionKind.ToggleRecent, Anchor: target.Bounds);
            case PanelTargetKind.ClearRecent:
                return new(PanelActionKind.ClearRecent, Anchor: target.Bounds);
            case PanelTargetKind.Documentation:
                return new(PanelActionKind.Documentation, Anchor: target.Bounds);
            case PanelTargetKind.Settings:
                return new(PanelActionKind.Settings, Anchor: target.Bounds);
            case PanelTargetKind.Quit:
                return new(PanelActionKind.Quit, Anchor: target.Bounds);
            default:
                return null;
        }
    }

    private TrayPanelScene Build()
    {
        var palette = _palette;
        var state = _state;
        var live = state.Discovery == DiscoveryState.Live;
        var commands = new List<PanelCommand>();
        var targets = new List<PanelHitTarget>();

        // Lay out the list first: its height decides the panel height and the scroll range.
        var items = new List<ListItem>();
        var y = ListTopPadding;
        var running = state.AppHosts.Where(host => host.Id.AppHostPid != 0).ToList();
        var pinned = state.AppHosts.Where(host => host.Id.AppHostPid == 0).ToList();
        if (state.AppHosts.Count == 0)
        {
            items.Add(new(ListItemKind.Empty, y, EmptyStateHeight, PanelSection.None, null));
            y += EmptyStateHeight;
        }
        AddSection(PanelSection.Running, running, expanded: true);
        AddSection(PanelSection.Pinned, pinned, expanded: true);
        AddSection(PanelSection.Recent, state.RecentAppHosts, IsRecentExpanded);
        _contentHeight = y + ListBottomPadding;

        void AddSection(PanelSection section, IReadOnlyList<AppHostMenuItem> hosts, bool expanded)
        {
            if (hosts.Count == 0)
            {
                return;
            }
            items.Add(new(ListItemKind.SectionHeader, y, SectionHeaderHeight, section, null));
            y += SectionHeaderHeight;
            if (!expanded)
            {
                return;
            }
            foreach (var host in hosts)
            {
                items.Add(new(ListItemKind.Row, y, RowHeight, section, host));
                y += RowHeight;
            }
        }

        var notice = string.IsNullOrWhiteSpace(state.Notice) ? null : state.Notice.Trim();
        var noticeSingleLine = notice is not null && notice.Length * AverageBodyCharacterWidth <= NoticeTextWidth;
        var noticeHeight = notice is null ? 0 : (noticeSingleLine ? 34 : 50) + 8;
        var chrome = HeaderHeight + noticeHeight + FooterHeight;
        var height = Math.Min(_maxHeight, chrome + _contentHeight);
        _viewport = new(0, HeaderHeight + noticeHeight, Width, Math.Max(0, height - chrome));
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _contentHeight - _viewport.Height));
        var listOrigin = _viewport.Y - _scroll;

        // Panel background and footer strip. The window itself is transparent (macOS) or
        // rounded by DWM or a region (Windows), so the panel draws its own rounded body.
        var panel = new PanelRect(0, 0, Width, height);
        var footer = new PanelRect(0, height - FooterHeight, Width, FooterHeight);
        commands.Add(new FillPathCommand(PanelPath.RoundedRect(panel, CornerRadius), palette.Background));
        commands.Add(new FillPathCommand(PanelPath.RoundedRect(footer, 0, 0, CornerRadius, CornerRadius), palette.Gutter));
        commands.Add(new FillPathCommand(PanelPath.Rectangle(new(0, footer.Y, Width, 1)), palette.Separator));

        AddHeader(commands, state, palette);
        if (notice is not null)
        {
            AddNotice(commands, notice, noticeSingleLine, noticeHeight - 8, palette);
        }
        if (_scroll > 0)
        {
            // Mark the edge only when content is scrolled under the header.
            commands.Add(new FillPathCommand(PanelPath.Rectangle(new(0, _viewport.Y, Width, 1)), palette.Separator));
        }

        var duplicateNames = running.GroupBy(host => host.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        commands.Add(new PushClipCommand(_viewport));
        var hidden = new List<PanelCommand>();
        foreach (var item in items)
        {
            var top = listOrigin + item.Y;
            // Items scrolled out of view are not drawn but keep their targets (with empty hit
            // bounds), so keyboard navigation can reach them and scroll them into view.
            var sink = top + item.Height <= _viewport.Y || top >= _viewport.Bottom ? hidden : commands;
            switch (item.Kind)
            {
                case ListItemKind.Empty:
                    AddEmptyState(sink, state.Discovery, top, palette);
                    break;
                case ListItemKind.SectionHeader:
                    AddSectionHeader(sink, targets, item.Section, top, state, palette);
                    break;
                case ListItemKind.Row:
                    AddRow(sink, targets, item.Host!, item.Section, top, live,
                        item.Section == PanelSection.Running && duplicateNames.Contains(item.Host!.DisplayName), palette);
                    break;
            }
        }
        commands.Add(new PopClipCommand());

        if (_contentHeight > _viewport.Height && _viewport.Height > 0)
        {
            var thumb = Math.Max(24, _viewport.Height * _viewport.Height / _contentHeight);
            var travel = _viewport.Height - thumb - 8;
            var thumbY = _viewport.Y + 4 + travel * _scroll / (_contentHeight - _viewport.Height);
            commands.Add(new FillPathCommand(PanelPath.RoundedRect(new(Width - 7, thumbY, 4, thumb), 2), palette.TextTertiary.WithAlpha(0.4)));
        }

        AddFooter(commands, targets, footer, palette);
        commands.Add(new StrokePathCommand(PanelPath.RoundedRect(panel.Inset(0.5, 0.5), CornerRadius - 0.5), palette.Border, 1));

        // Forget hover and focus on targets that disappeared, such as a row that was removed.
        if (_focus is { } focus && !targets.Any(target => target.Key == focus))
        {
            _focus = null;
            _focusVisible = false;
        }
        if (_hover is { } hover && !targets.Any(target => target.Key == hover))
        {
            _hover = null;
        }
        if (_focusVisible && targets.FirstOrDefault(target => target.Key == _focus) is { } focused)
        {
            var clip = IsFooter(focused.Kind) ? panel : _viewport;
            commands.Add(new PushClipCommand(clip));
            commands.Add(new StrokePathCommand(PanelPath.RoundedRect(focused.Bounds.Inset(-1, -1), 7), palette.Focus, 2));
            commands.Add(new PopClipCommand());
        }

        return new(Width, height, commands, targets);
    }

    private const double NoticeTextWidth = Width - 46 - 22;

    private static void AddHeader(List<PanelCommand> commands, TrayViewState state, TrayPanelPalette palette)
    {
        commands.Add(new LogoCommand(new(16, 16, 28, 28)));
        commands.Add(new TextCommand(new(54, 12, Width - 54 - 16, 20), "Aspire", PanelFontRole.Title, palette.TextPrimary));
        var (status, dot) = GetHeaderStatus(state, palette);
        commands.Add(new FillPathCommand(PanelPath.Circle(58, 40, 4), dot));
        commands.Add(new TextCommand(new(68, 32, Width - 68 - 16, 16), status, PanelFontRole.Body, palette.TextSecondary));
    }

    private static void AddNotice(List<PanelCommand> commands, string notice, bool singleLine, double height, TrayPanelPalette palette)
    {
        var banner = new PanelRect(12, HeaderHeight, Width - 24, height);
        commands.Add(new FillPathCommand(PanelPath.RoundedRect(banner, 6), palette.Banner));
        if (palette.IsHighContrast)
        {
            commands.Add(new StrokePathCommand(PanelPath.RoundedRect(banner.Inset(0.5, 0.5), 5.5), palette.Border, 1));
        }
        TrayPanelGlyphs.AddIcon(commands, PanelIcon.Alert, new(banner.X + 10, banner.Y + 9, 16, 16), palette.BannerText);
        var text = new PanelRect(banner.X + 34, banner.Y + 8, NoticeTextWidth, height - 16);
        commands.Add(singleLine
            ? new TextCommand(text, notice, PanelFontRole.Body, palette.BannerText)
            : new TextCommand(text, notice, PanelFontRole.Body, palette.BannerText, MaxLines: 2));
    }

    private static void AddEmptyState(List<PanelCommand> commands, DiscoveryState discovery, double top, TrayPanelPalette palette)
    {
        var (title, hint) = discovery switch
        {
            DiscoveryState.Live => ("No AppHosts running", "Run “aspire run” in a project, or pin one to keep it here."),
            DiscoveryState.Connecting => ("Looking for AppHosts…", "Connecting to the Aspire CLI."),
            _ => ("AppHosts unavailable", "Running AppHosts will appear here once discovery is back.")
        };
        var art = new PanelRect(Width / 2 - 16, top + 20, 32, 32);
        if (discovery == DiscoveryState.Connecting)
        {
            TrayPanelGlyphs.AddStatus(commands, PanelStatusGlyph.Busy, art.Inset(2, 2), palette);
        }
        else
        {
            TrayPanelGlyphs.AddIcon(commands, PanelIcon.Layers, art, palette.TextTertiary);
        }
        commands.Add(new TextCommand(new(20, top + 62, Width - 40, 20), title, PanelFontRole.Name, palette.TextPrimary, PanelTextAlignment.Center));
        commands.Add(new TextCommand(new(44, top + 84, Width - 88, 34), hint, PanelFontRole.Body, palette.TextSecondary,
            PanelTextAlignment.Center, MaxLines: 2));
    }

    private void AddSectionHeader(List<PanelCommand> commands, List<PanelHitTarget> targets, PanelSection section, double top,
        TrayViewState state, TrayPanelPalette palette)
    {
        var label = section switch
        {
            PanelSection.Running => "RUNNING",
            PanelSection.Pinned => "PINNED",
            _ => $"RECENT · {state.RecentAppHosts.Count}"
        };
        var labelBounds = new PanelRect(20, top + 4, 200, 24);
        if (section != PanelSection.Recent)
        {
            commands.Add(new TextCommand(labelBounds, label, PanelFontRole.Section, palette.TextTertiary));
            return;
        }

        var expanded = IsRecentExpanded;
        var toggle = new PanelRect(RowInset, top + 4, Width - 2 * RowInset, 24);
        var toggleKey = new PanelTargetKey(PanelTargetKind.ToggleRecent, section, null);
        AddInteractionFill(commands, toggleKey, toggle, palette, includeFocus: false);
        commands.Add(new TextCommand(labelBounds, label, PanelFontRole.Section, palette.TextTertiary));
        TrayPanelGlyphs.AddIcon(commands, expanded ? PanelIcon.ChevronDown : PanelIcon.ChevronRight,
            new(RowContentRight - 22, top + 8, 16, 16), palette.TextTertiary);
        AddTarget(targets, toggleKey, toggle, expanded ? "Hide recent AppHosts" : "Show recent AppHosts", true, null);

        if (state.CanClearRecent)
        {
            var clear = new PanelRect(RowContentRight - 30 - 52, top + 6, 52, 20);
            var clearKey = new PanelTargetKey(PanelTargetKind.ClearRecent, section, null);
            AddInteractionFill(commands, clearKey, clear, palette, includeFocus: false);
            commands.Add(new TextCommand(clear, "Clear", PanelFontRole.Body, palette.Accent, PanelTextAlignment.Center));
            AddTarget(targets, clearKey, clear, "Clear recent AppHosts", true, null);
        }
    }

    private void AddRow(List<PanelCommand> commands, List<PanelHitTarget> targets, AppHostMenuItem host, PanelSection section,
        double top, bool live, bool showPid, TrayPanelPalette palette)
    {
        var rowKey = new PanelTargetKey(PanelTargetKind.Row, section, host.Id);
        var row = new PanelRect(RowInset, top + 2, Width - 2 * RowInset, RowHeight - 4);
        var active = IsWithinRow(_hover, rowKey) || IsWithinRow(_pressed, rowKey) || (_focusVisible && IsWithinRow(_focus, rowKey));
        if (_pressed == rowKey)
        {
            commands.Add(new FillPathCommand(PanelPath.RoundedRect(row, 6), palette.Pressed));
        }
        else if (active)
        {
            commands.Add(new FillPathCommand(PanelPath.RoundedRect(row, 6), palette.Hover));
        }

        TrayPanelGlyphs.AddStatus(commands, GetStatusGlyph(host, live), new(20, top + 16, 20, 20), palette);

        var buttons = host.Id.AppHostPid != 0
            ? new[] { PanelTargetKind.Dashboard, PanelTargetKind.Stop, PanelTargetKind.More }
            : [PanelTargetKind.Start, PanelTargetKind.More];
        var textRight = RowContentRight - buttons.Length * ButtonSize - (buttons.Length - 1) * ButtonGap - 8;
        var (detail, isError) = GetRowDetail(host, live, showPid);
        commands.Add(new TextCommand(new(TextLeft, top + 8, textRight - TextLeft, 18), host.DisplayName, PanelFontRole.Name, palette.TextPrimary));
        commands.Add(new TextCommand(new(TextLeft, top + 27, textRight - TextLeft, 16), detail, PanelFontRole.Body,
            isError ? palette.Danger : palette.TextSecondary, Truncation: isError ? PanelTruncation.End : PanelTruncation.Middle));

        AddTarget(targets, rowKey, row, GetRowAccessibleName(host, detail), true, host);
        var x = RowContentRight - buttons.Length * ButtonSize - (buttons.Length - 1) * ButtonGap;
        foreach (var kind in buttons)
        {
            var bounds = new PanelRect(x, top + (RowHeight - ButtonSize) / 2, ButtonSize, ButtonSize);
            x += ButtonSize + ButtonGap;
            var key = rowKey with { Kind = kind };
            var (icon, name, enabled) = kind switch
            {
                PanelTargetKind.Dashboard => (PanelIcon.ExternalLink, $"Open dashboard for {host.DisplayName}", host.CanOpenDashboard),
                PanelTargetKind.Stop => (PanelIcon.Stop, $"Stop {host.DisplayName}", host.CanStop),
                PanelTargetKind.Start => (PanelIcon.Play, $"Start {host.DisplayName}", host.CanStart),
                _ => (PanelIcon.More, $"More actions for {host.DisplayName}", true)
            };
            // Buttons always keep their space so the text never reflows on hover, and they
            // stay hit-testable: the pointer is inside the row, so they are visible, before any click.
            AddTarget(targets, key, bounds, name, enabled, host);
            if (!active)
            {
                continue;
            }
            if (enabled)
            {
                AddInteractionFill(commands, key, bounds, palette, includeFocus: false);
            }
            var color = !enabled ? palette.TextTertiary.WithAlpha(0.45)
                : _hover == key || _pressed == key ? palette.TextPrimary
                : palette.TextSecondary;
            TrayPanelGlyphs.AddIcon(commands, icon, bounds.Inset(6, 6), color);
        }
    }

    private void AddFooter(List<PanelCommand> commands, List<PanelHitTarget> targets, PanelRect footer, TrayPanelPalette palette)
    {
        var docs = new PanelRect(8, footer.Y + 8, 140, ButtonSize);
        var docsKey = new PanelTargetKey(PanelTargetKind.Documentation, PanelSection.None, null);
        AddInteractionFill(commands, docsKey, docs, palette, includeFocus: false);
        TrayPanelGlyphs.AddIcon(commands, PanelIcon.Book, new(docs.X + 8, docs.Y + 6, 16, 16), palette.TextSecondary);
        commands.Add(new TextCommand(new(docs.X + 32, docs.Y, docs.Width - 40, docs.Height), "Documentation", PanelFontRole.Body, palette.TextSecondary));
        AddTarget(targets, docsKey, docs, "Open Aspire documentation", true, null);

        foreach (var (kind, icon, name, x) in new[]
        {
            (PanelTargetKind.Settings, PanelIcon.Gear, "Settings", RowContentRight + 4 - 2 * ButtonSize - ButtonGap),
            (PanelTargetKind.Quit, PanelIcon.Power, "Quit Aspire", RowContentRight + 4 - ButtonSize)
        })
        {
            var bounds = new PanelRect(x, footer.Y + 8, ButtonSize, ButtonSize);
            var key = new PanelTargetKey(kind, PanelSection.None, null);
            AddInteractionFill(commands, key, bounds, palette, includeFocus: false);
            TrayPanelGlyphs.AddIcon(commands, icon, bounds.Inset(6, 6), _hover == key ? palette.TextPrimary : palette.TextSecondary);
            AddTarget(targets, key, bounds, name, true, null);
        }
    }

    private void AddInteractionFill(List<PanelCommand> commands, PanelTargetKey key, PanelRect bounds, TrayPanelPalette palette, bool includeFocus)
    {
        var color = _pressed == key ? palette.Pressed
            : _hover == key || (includeFocus && _focusVisible && _focus == key) ? palette.Hover
            : (PanelColor?)null;
        if (color is { } fill)
        {
            commands.Add(new FillPathCommand(PanelPath.RoundedRect(bounds, 6), fill));
        }
    }

    private void AddTarget(List<PanelHitTarget> targets, PanelTargetKey key, PanelRect bounds, string name, bool enabled, AppHostMenuItem? host)
    {
        var hit = IsFooter(key.Kind) ? bounds : bounds.Intersect(_viewport);
        targets.Add(new(key, bounds, hit, name, enabled, host));
    }

    private static bool IsFooter(PanelTargetKind kind)
        => kind is PanelTargetKind.Documentation or PanelTargetKind.Settings or PanelTargetKind.Quit;

    private static bool IsWithinRow(PanelTargetKey? key, PanelTargetKey row)
        => key is { } value && value.Host == row.Host && value.Section == row.Section
            && value.Kind is PanelTargetKind.Row or PanelTargetKind.Dashboard or PanelTargetKind.Stop
                or PanelTargetKind.Start or PanelTargetKind.More;

    internal static PanelStatusGlyph GetStatusGlyph(AppHostMenuItem host, bool live) => host switch
    {
        { IsStarting: true } or { IsStopping: true } => PanelStatusGlyph.Busy,
        { IsRunning: false } => host.Error is null ? PanelStatusGlyph.Stopped : PanelStatusGlyph.Warning,
        _ when !live => PanelStatusGlyph.Unknown,
        { Error: not null } => PanelStatusGlyph.Warning,
        { Health: AppHostHealth.Healthy } => PanelStatusGlyph.Healthy,
        { Health: AppHostHealth.Warning } => PanelStatusGlyph.Warning,
        { Health: AppHostHealth.Unhealthy } => PanelStatusGlyph.Unhealthy,
        _ => PanelStatusGlyph.Unknown
    };

    /// <summary>
    /// Returns the second line of a row: <c>{short status} · {parent/dir}</c>, with the PID only when
    /// two running rows share a display name. Errors replace the whole line.
    /// </summary>
    internal static (string Text, bool IsError) GetRowDetail(AppHostMenuItem host, bool live, bool showPid)
    {
        if (host.Error is { } error)
        {
            return (error, true);
        }
        var status = host switch
        {
            { IsStarting: true } => "Starting…",
            { IsStopping: true } => "Stopping…",
            // Offline rows carry the controller's source-file probe ("AppHost source file not found").
            { IsRunning: false } when host.Id.AppHostPid == 0 && host.Subtitle != "Stopped" => host.Subtitle,
            { IsRunning: false } => "Stopped",
            _ when !live => "Status unavailable",
            { Health: AppHostHealth.Warning } => "Needs attention",
            { Health: AppHostHealth.Unhealthy } => "Unhealthy",
            _ => "Running"
        };
        var detail = $"{status} · {AppHostPresentation.GetLocation(host.Id.AppHostPath)}";
        return (showPid ? $"{detail} · PID {host.Id.AppHostPid}" : detail, false);
    }

    private static string GetRowAccessibleName(AppHostMenuItem host, string detail) => $"{host.DisplayName}, {detail}";

    /// <summary>Returns the concise header status line and the color of its connection dot.</summary>
    internal static (string Text, PanelColor Dot) GetHeaderStatus(TrayViewState state, TrayPanelPalette palette)
    {
        switch (state.Discovery)
        {
            case DiscoveryState.Connecting:
                return ("Connecting…", palette.Neutral);
            case DiscoveryState.Disconnected:
                return ("Reconnecting…", palette.Warning);
            case DiscoveryState.Incompatible:
                return ("Incompatible CLI", palette.Danger);
            case DiscoveryState.LimitExceeded:
                return ("Discovery limit exceeded", palette.Danger);
        }
        var running = state.AppHosts.Count(host => host.IsRunning && host.Id.AppHostPid != 0);
        var starting = state.AppHosts.Concat(state.RecentAppHosts).Count(host => host.IsStarting);
        var attention = state.AppHosts.Concat(state.RecentAppHosts).Count(host => host.Error is not null
            || (host.IsRunning && host.Id.AppHostPid != 0 && host.Health is AppHostHealth.Warning or AppHostHealth.Unhealthy));
        var parts = new List<string>();
        if (running > 0)
        {
            parts.Add($"{running} running");
        }
        if (starting > 0)
        {
            parts.Add($"{starting} starting");
        }
        if (attention > 0)
        {
            parts.Add($"{attention} need{(attention == 1 ? "s" : "")} attention");
        }
        return (parts.Count == 0 ? "No AppHosts running" : string.Join(" · ", parts), palette.Success);
    }

    private enum ListItemKind
    {
        Empty,
        SectionHeader,
        Row
    }

    private readonly record struct ListItem(ListItemKind Kind, double Y, double Height, PanelSection Section, AppHostMenuItem? Host);
}
