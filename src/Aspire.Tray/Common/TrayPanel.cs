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
    public const double ConfirmHeight = 92;
    public const double SettingsRowHeight = 62;
    public const double AboutHeight = 84;

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
    private PanelConfirm? _confirm;
    private PanelSettings? _settings;
    private double _listScroll;

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

    /// <summary>The inline confirmation being shown, if any.</summary>
    public PanelConfirmKind? Confirmation => _confirm?.Kind;

    /// <summary>Whether the confirmation strip's "Don't ask again" toggle is on.</summary>
    public bool ConfirmationDontAskAgain => _confirm?.DontAskAgain ?? false;

    /// <summary>Whether the panel shows its Settings view instead of the AppHost list.</summary>
    public bool IsSettingsOpen => _settings is not null;

    /// <summary>
    /// Switches to the Settings view, or refreshes it when it is already open. Focus starts on
    /// the back arrow so Enter or Escape returns to the list.
    /// </summary>
    public PanelResponse ShowSettings(PanelSettings settings)
    {
        if (_settings is null)
        {
            _confirm = null;
            _listScroll = _scroll;
            _scroll = 0;
            _focus = new(PanelTargetKind.Back, PanelSection.None, null);
        }
        _settings = settings;
        RefreshHover();
        return PanelResponse.Changed;
    }

    /// <summary>Refreshes the Settings view after a toggle was applied; ignored when it is closed.</summary>
    public PanelResponse UpdateSettings(PanelSettings settings)
    {
        if (_settings is null)
        {
            return PanelResponse.None;
        }
        _settings = settings;
        _scene = null;
        return PanelResponse.Changed;
    }

    /// <summary>
    /// Replaces a recent row with a prompt to remove it, after the host found its source file
    /// missing on Start. Confirming returns <see cref="PanelActionKind.RemoveRecent"/>.
    /// </summary>
    public PanelResponse ConfirmRemoveMissing(AppHostMenuItem host)
    {
        _settings = null;
        _recentExpanded = true;
        BeginConfirm(new(PanelConfirmKind.RemoveMissing, PanelSection.Recent, host));
        RefreshHover();
        return PanelResponse.Changed;
    }

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
        _listScroll = 0;
        _confirm = null;
        _settings = null;
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
                // Escape steps back one level: out of a confirmation, then out of Settings,
                // and only then closes the panel.
                if (_confirm is not null)
                {
                    CancelConfirm();
                    return PanelResponse.Changed;
                }
                if (_settings is not null)
                {
                    CloseSettings();
                    return PanelResponse.Changed;
                }
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
            or PanelTargetKind.Settings or PanelTargetKind.Quit or PanelTargetKind.ConfirmCancel or PanelTargetKind.Back
            or PanelTargetKind.LaunchAtSignIn or PanelTargetKind.ConfirmStopSetting;

    private static bool IsConfirmTarget(PanelTargetKind kind)
        => kind is PanelTargetKind.ConfirmCancel or PanelTargetKind.ConfirmAccept or PanelTargetKind.ConfirmDontAsk;

    private void BeginConfirm(PanelConfirm confirm)
    {
        _confirm = confirm;
        // Focus starts on Cancel, so Enter or Space right after the click never confirms.
        _focus = new(PanelTargetKind.ConfirmCancel, confirm.Section, confirm.Host?.Id);
        _scene = null;

        // The strip is taller than the row it replaces and may be below the fold (a recent row
        // reached by scrolling), so scroll the whole strip into view.
        var buttons = Scene.Targets.Where(target => IsConfirmTarget(target.Kind)).ToList();
        if (buttons.Count > 0)
        {
            var bottom = buttons.Max(target => target.Bounds.Bottom) + 14;
            var top = bottom - ConfirmHeight;
            if (bottom > _viewport.Bottom)
            {
                _scroll += bottom - _viewport.Bottom;
            }
            if (top < _viewport.Y)
            {
                _scroll -= _viewport.Y - top;
            }
            _scene = null;
        }
    }

    private void CancelConfirm()
    {
        if (_confirm is not { } confirm)
        {
            return;
        }
        _confirm = null;
        // Return focus to the control that opened the confirmation.
        _focus = confirm.Kind switch
        {
            PanelConfirmKind.Stop => new(PanelTargetKind.Stop, confirm.Section, confirm.Host?.Id),
            PanelConfirmKind.ClearRecent => new(PanelTargetKind.ClearRecent, confirm.Section, null),
            _ => new(PanelTargetKind.Start, confirm.Section, confirm.Host?.Id)
        };
        _scene = null;
    }

    private void CloseSettings()
    {
        _settings = null;
        _scroll = _listScroll;
        _focus = new(PanelTargetKind.Settings, PanelSection.None, null);
        RefreshHover();
    }

    /// <summary>Hit tests the pointer again after the layout changed under it.</summary>
    private void RefreshHover()
    {
        _scene = null;
        if (_pointer is { } pointer)
        {
            _hover = HitTest(pointer.X, pointer.Y)?.Key;
            _scene = null;
        }
    }

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
        if (IsChrome(target.Kind))
        {
            // Header and footer buttons never scroll.
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
        if (_confirm is not null && !IsConfirmTarget(target.Kind))
        {
            // Acting on anything else abandons the pending confirmation.
            _confirm = null;
            _scene = null;
        }
        switch (target.Kind)
        {
            case PanelTargetKind.Row when host is { CanOpenDashboard: true }:
            case PanelTargetKind.Dashboard:
                return new(PanelActionKind.OpenDashboard, host, target.Bounds) { Recent = recent };
            case PanelTargetKind.Row:
                // Rows without a dashboard open the same menu as their "More" button.
                var more = Scene.Targets.FirstOrDefault(other => other.Key == target.Key with { Kind = PanelTargetKind.More });
                return new(PanelActionKind.More, host, more?.Bounds ?? target.Bounds) { Recent = recent };
            case PanelTargetKind.Stop when _state.ConfirmStop:
                BeginConfirm(new(PanelConfirmKind.Stop, target.Key.Section, host));
                return null;
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
                BeginConfirm(new(PanelConfirmKind.ClearRecent, PanelSection.Recent, null));
                return null;
            case PanelTargetKind.ConfirmCancel:
                CancelConfirm();
                return null;
            case PanelTargetKind.ConfirmDontAsk when _confirm is { } pending:
                _confirm = pending with { DontAskAgain = !pending.DontAskAgain };
                _scene = null;
                return null;
            case PanelTargetKind.ConfirmAccept when _confirm is { } confirmed:
                _confirm = null;
                _scene = null;
                return confirmed.Kind switch
                {
                    PanelConfirmKind.Stop => new(PanelActionKind.Stop, host, target.Bounds) { DontAskAgain = confirmed.DontAskAgain },
                    PanelConfirmKind.ClearRecent => new(PanelActionKind.ClearRecent, Anchor: target.Bounds),
                    _ => new(PanelActionKind.RemoveRecent, host, target.Bounds) { Recent = true }
                };
            case PanelTargetKind.Back:
            case PanelTargetKind.Settings when _settings is not null:
                CloseSettings();
                return null;
            case PanelTargetKind.LaunchAtSignIn:
                return new(PanelActionKind.SetLaunchAtSignIn, Anchor: target.Bounds) { Enabled = _settings?.Startup?.Enabled != true };
            case PanelTargetKind.ConfirmStopSetting:
                return new(PanelActionKind.SetConfirmStop, Anchor: target.Bounds) { Enabled = !_state.ConfirmStop };
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
        if (_settings is { } settings)
        {
            AddSettingsItems(settings);
        }
        else
        {
            RevalidateConfirm(running);
            if (state.AppHosts.Count == 0)
            {
                Add(ListItemKind.Empty, EmptyStateHeight, PanelSection.None, null);
            }
            AddSection(PanelSection.Running, running, expanded: true);
            AddSection(PanelSection.Pinned, pinned, expanded: true);
            AddSection(PanelSection.Recent, state.RecentAppHosts, IsRecentExpanded);
        }
        _contentHeight = y + ListBottomPadding;

        void Add(ListItemKind kind, double height, PanelSection section, AppHostMenuItem? host, PanelTargetKind target = default)
        {
            items.Add(new(kind, y, height, section, host, target));
            y += height;
        }

        void AddSection(PanelSection section, IReadOnlyList<AppHostMenuItem> hosts, bool expanded)
        {
            if (hosts.Count == 0)
            {
                return;
            }
            // An inline confirmation takes the place of the item it is about, so the list
            // keeps its order and the strip sits exactly where the user clicked.
            if (section == PanelSection.Recent && _confirm is { Kind: PanelConfirmKind.ClearRecent })
            {
                Add(ListItemKind.Confirm, ConfirmHeight, section, null);
            }
            else
            {
                Add(ListItemKind.SectionHeader, SectionHeaderHeight, section, null);
            }
            if (!expanded)
            {
                return;
            }
            foreach (var host in hosts)
            {
                var confirming = _confirm is { Kind: PanelConfirmKind.Stop or PanelConfirmKind.RemoveMissing } confirm
                    && confirm.Section == section && confirm.Host?.Id == host.Id;
                Add(confirming ? ListItemKind.Confirm : ListItemKind.Row, confirming ? ConfirmHeight : RowHeight, section, host);
            }
        }

        void AddSettingsItems(PanelSettings settings)
        {
            Add(ListItemKind.SectionHeader, SectionHeaderHeight, PanelSection.None, null, PanelTargetKind.LaunchAtSignIn);
            Add(ListItemKind.SettingsToggle, SettingsRowHeight, PanelSection.None, null, PanelTargetKind.LaunchAtSignIn);
            Add(ListItemKind.SettingsToggle, SettingsRowHeight, PanelSection.None, null, PanelTargetKind.ConfirmStopSetting);
            Add(ListItemKind.SectionHeader, SectionHeaderHeight, PanelSection.None, null, PanelTargetKind.Documentation);
            Add(ListItemKind.About, AboutHeight, PanelSection.None, null);
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

        if (_settings is null)
        {
            AddHeader(commands, state, palette);
        }
        else
        {
            AddSettingsHeader(commands, targets, palette);
        }
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
                case ListItemKind.SectionHeader when _settings is not null:
                    sink.Add(new TextCommand(new(20, top + 4, 200, 24),
                        item.Target == PanelTargetKind.LaunchAtSignIn ? "GENERAL" : "ABOUT", PanelFontRole.Section, palette.TextTertiary));
                    break;
                case ListItemKind.SectionHeader:
                    AddSectionHeader(sink, targets, item.Section, top, state, palette);
                    break;
                case ListItemKind.Row:
                    AddRow(sink, targets, item.Host!, item.Section, top, live,
                        item.Section == PanelSection.Running && duplicateNames.Contains(item.Host!.DisplayName), palette);
                    break;
                case ListItemKind.Confirm:
                    AddConfirm(sink, targets, _confirm!, item.Host, top, palette);
                    break;
                case ListItemKind.SettingsToggle:
                    AddSettingsToggle(sink, targets, item.Target, _settings!, top, palette);
                    break;
                case ListItemKind.About:
                    AddAbout(sink, _settings!, top, palette);
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
            var clip = IsChrome(focused.Kind) ? panel : _viewport;
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

    /// <summary>
    /// Drops a confirmation whose subject went away (the AppHost stopped, or the recent list was
    /// cleared elsewhere) and picks up the subject's latest state for the strip's text.
    /// </summary>
    private void RevalidateConfirm(List<AppHostMenuItem> running)
    {
        if (_confirm is not { } confirm)
        {
            return;
        }
        var current = confirm.Kind switch
        {
            PanelConfirmKind.Stop => running.FirstOrDefault(host => host.Id == confirm.Host!.Id && host.CanStop),
            PanelConfirmKind.RemoveMissing => _state.RecentAppHosts.FirstOrDefault(host => host.Id == confirm.Host!.Id),
            _ => null
        };
        var valid = confirm.Kind == PanelConfirmKind.ClearRecent ? _state.CanClearRecent : current is not null;
        if (!valid)
        {
            CancelConfirm();
        }
        else if (current is not null)
        {
            _confirm = confirm with { Host = current };
        }
    }

    /// <summary>
    /// Draws an inline confirmation strip: a question, one line of detail, and a row of controls
    /// (an optional "Don't ask again" toggle, then Cancel and the destructive button).
    /// </summary>
    private void AddConfirm(List<PanelCommand> commands, List<PanelHitTarget> targets, PanelConfirm confirm, AppHostMenuItem? host,
        double top, TrayPanelPalette palette)
    {
        var card = new PanelRect(RowInset, top + 4, Width - 2 * RowInset, ConfirmHeight - 8);
        commands.Add(new FillPathCommand(PanelPath.RoundedRect(card, 8), palette.Gutter));
        commands.Add(new StrokePathCommand(PanelPath.RoundedRect(card.Inset(0.5, 0.5), 7.5), palette.Border, 1));

        var name = host?.DisplayName ?? "";
        var (question, detail, accept, acceptName) = confirm.Kind switch
        {
            PanelConfirmKind.Stop => ($"Stop {name}?", "Persistent resources keep running.", "Stop", $"Stop {name}"),
            PanelConfirmKind.ClearRecent => ("Clear recent AppHosts?", "Pinned AppHosts are kept.", "Clear", "Clear recent AppHosts"),
            _ => ($"{name} was not found", host!.Id.AppHostPath, "Remove", $"Remove {name} from recent AppHosts")
        };
        var textWidth = card.Width - 24;
        commands.Add(new TextCommand(new(card.X + 12, card.Y + 9, textWidth, 18), question, PanelFontRole.Name, palette.TextPrimary));
        commands.Add(new TextCommand(new(card.X + 12, card.Y + 28, textWidth, 16), detail, PanelFontRole.Body, palette.TextSecondary,
            Truncation: confirm.Kind == PanelConfirmKind.RemoveMissing ? PanelTruncation.Middle : PanelTruncation.End));

        var controlsY = card.Bottom - 10 - ButtonSize;
        var section = confirm.Section;
        var id = host?.Id;
        if (confirm.Kind == PanelConfirmKind.Stop)
        {
            var toggle = new PanelRect(card.X + 4, controlsY, 140, ButtonSize);
            var toggleKey = new PanelTargetKey(PanelTargetKind.ConfirmDontAsk, section, id);
            AddInteractionFill(commands, toggleKey, toggle, palette, includeFocus: false);
            TrayPanelGlyphs.AddToggle(commands, confirm.DontAskAgain, enabled: true, new(toggle.X + 8, toggle.Y + 7, 25, 14), palette);
            commands.Add(new TextCommand(new(toggle.X + 40, toggle.Y, toggle.Width - 44, toggle.Height), "Don't ask again",
                PanelFontRole.Body, palette.TextSecondary));
            AddTarget(targets, toggleKey, toggle, confirm.DontAskAgain ? "Don't ask again, on" : "Don't ask again, off", true, host);
        }

        const double ButtonWidth = 64;
        var acceptBounds = new PanelRect(card.Right - 10 - ButtonWidth, controlsY, ButtonWidth, ButtonSize);
        var cancelBounds = new PanelRect(acceptBounds.X - 8 - ButtonWidth, controlsY, ButtonWidth, ButtonSize);
        var cancelKey = new PanelTargetKey(PanelTargetKind.ConfirmCancel, section, id);
        var acceptKey = new PanelTargetKey(PanelTargetKind.ConfirmAccept, section, id);

        // Cancel is a neutral outlined button; the destructive one is filled with the danger color.
        commands.Add(new FillPathCommand(PanelPath.RoundedRect(cancelBounds, 6), palette.Background));
        AddInteractionFill(commands, cancelKey, cancelBounds, palette, includeFocus: false);
        commands.Add(new StrokePathCommand(PanelPath.RoundedRect(cancelBounds.Inset(0.5, 0.5), 5.5), palette.Border, 1));
        commands.Add(new TextCommand(cancelBounds, "Cancel", PanelFontRole.Button, palette.TextPrimary, PanelTextAlignment.Center));
        AddTarget(targets, cancelKey, cancelBounds, "Cancel", true, host);

        commands.Add(new FillPathCommand(PanelPath.RoundedRect(acceptBounds, 6), palette.Danger));
        if (_pressed == acceptKey || _hover == acceptKey)
        {
            // Darken the filled button: the shared hover tints are tuned for the panel background.
            commands.Add(new FillPathCommand(PanelPath.RoundedRect(acceptBounds, 6),
                PanelColor.FromRgba(0, 0, 0, _pressed == acceptKey ? 0.22 : 0.12)));
        }
        if (palette.IsHighContrast)
        {
            commands.Add(new StrokePathCommand(PanelPath.RoundedRect(acceptBounds.Inset(0.5, 0.5), 5.5), palette.Border, 1));
        }
        commands.Add(new TextCommand(acceptBounds, accept, PanelFontRole.Button, palette.OnColor, PanelTextAlignment.Center));
        AddTarget(targets, acceptKey, acceptBounds, acceptName, true, host);
    }

    private void AddSettingsHeader(List<PanelCommand> commands, List<PanelHitTarget> targets, TrayPanelPalette palette)
    {
        var back = new PanelRect(12, (HeaderHeight - ButtonSize) / 2, ButtonSize, ButtonSize);
        var backKey = new PanelTargetKey(PanelTargetKind.Back, PanelSection.None, null);
        AddInteractionFill(commands, backKey, back, palette, includeFocus: false);
        TrayPanelGlyphs.AddIcon(commands, PanelIcon.ChevronLeft, back.Inset(6, 6), _hover == backKey ? palette.TextPrimary : palette.TextSecondary);
        AddTarget(targets, backKey, back, "Back to AppHosts", true, null);
        commands.Add(new TextCommand(new(back.Right + 8, 20, Width - back.Right - 24, 20), TraySettingsText.PanelTitle,
            PanelFontRole.Title, palette.TextPrimary));
    }

    private void AddSettingsToggle(List<PanelCommand> commands, List<PanelHitTarget> targets, PanelTargetKind kind, PanelSettings settings,
        double top, TrayPanelPalette palette)
    {
        var row = new PanelRect(RowInset, top + 2, Width - 2 * RowInset, SettingsRowHeight - 4);
        var key = new PanelTargetKey(kind, PanelSection.None, null);
        bool on, enabled, error = false;
        string title, description;
        if (kind == PanelTargetKind.LaunchAtSignIn)
        {
            var startup = settings.Startup;
            on = startup?.Enabled == true;
            // Turning it off always works; turning it on needs a stable native installation.
            enabled = startup is not null && (on || startup.CanEnable);
            title = TraySettingsText.PanelStartupOption;
            var status = startup is null ? settings.StartupError ?? "" : TraySettingsText.GetStartupStatus(startup, settings.StartupError);
            error = settings.StartupError is not null;
            description = string.IsNullOrEmpty(status) ? TraySettingsText.StartupDescription : status.Replace(Environment.NewLine, " ");
        }
        else
        {
            on = _state.ConfirmStop;
            enabled = true;
            title = TraySettingsText.ConfirmStopOption;
            description = TraySettingsText.ConfirmStopDescription;
        }
        if (enabled)
        {
            AddInteractionFill(commands, key, row, palette, includeFocus: false);
        }
        var toggle = new PanelRect(RowContentRight - 36, top + 9, 32, 18);
        var textWidth = toggle.X - 12 - 20;
        commands.Add(new TextCommand(new(20, top + 8, textWidth, 20), title, PanelFontRole.Name,
            enabled ? palette.TextPrimary : palette.TextTertiary));
        commands.Add(new TextCommand(new(20, top + 28, textWidth, 30), description, PanelFontRole.Body,
            error ? palette.Danger : palette.TextSecondary, MaxLines: 2));
        TrayPanelGlyphs.AddToggle(commands, on, enabled, toggle, palette);
        AddTarget(targets, key, row, $"{title}, {(on ? "on" : "off")}", enabled, null);
    }

    private static void AddAbout(List<PanelCommand> commands, PanelSettings settings, double top, TrayPanelPalette palette)
    {
        commands.Add(new TextCommand(new(20, top + 4, Width - 40, 20), "Aspire Tray", PanelFontRole.Name, palette.TextPrimary));
        var lines = TraySettingsText.GetVersionText().Split(Environment.NewLine)
            .Append(TraySettingsText.GetAboutDescription(settings.Preview));
        var y = top + 24;
        foreach (var line in lines)
        {
            // Informational versions carry a long commit hash at the end; keep both ends visible.
            commands.Add(new TextCommand(new(20, y, Width - 40, 16), line, PanelFontRole.Body, palette.TextSecondary,
                Truncation: PanelTruncation.Middle));
            y += 18;
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
            (PanelTargetKind.Settings, PanelIcon.Gear, _settings is null ? "Settings" : "Close settings", RowContentRight + 4 - 2 * ButtonSize - ButtonGap),
            (PanelTargetKind.Quit, PanelIcon.Power, "Quit Aspire", RowContentRight + 4 - ButtonSize)
        })
        {
            var bounds = new PanelRect(x, footer.Y + 8, ButtonSize, ButtonSize);
            var key = new PanelTargetKey(kind, PanelSection.None, null);
            if (kind == PanelTargetKind.Settings && _settings is not null && _hover != key && _pressed != key)
            {
                // Mark the gear as selected while its view is open; clicking it again goes back.
                commands.Add(new FillPathCommand(PanelPath.RoundedRect(bounds, 6), palette.Pressed));
            }
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
        var hit = IsChrome(key.Kind) ? bounds : bounds.Intersect(_viewport);
        targets.Add(new(key, bounds, hit, name, enabled, host));
    }

    /// <summary>Targets in the fixed header or footer, outside the scrolled list.</summary>
    private static bool IsChrome(PanelTargetKind kind)
        => kind is PanelTargetKind.Documentation or PanelTargetKind.Settings or PanelTargetKind.Quit or PanelTargetKind.Back;

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
        Row,
        Confirm,
        SettingsToggle,
        About
    }

    /// <summary>One item of the scrolled list. <see cref="Target"/> tells Settings items apart.</summary>
    private readonly record struct ListItem(ListItemKind Kind, double Y, double Height, PanelSection Section, AppHostMenuItem? Host,
        PanelTargetKind Target);

    /// <summary>A pending inline confirmation. <see cref="Host"/> is null for Clear recent.</summary>
    private sealed record PanelConfirm(PanelConfirmKind Kind, PanelSection Section, AppHostMenuItem? Host, bool DontAskAgain = false);
}
