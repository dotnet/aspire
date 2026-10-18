// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Tray.Tests.Helpers;

namespace Aspire.Tray.Tests;

public class TrayPanelTests
{
    private static readonly string[] s_sampleNames =
    [
        "typical", "empty", "connecting", "errors", "disconnected", "many",
        "confirm-stop", "confirm-clear", "confirm-missing", "settings", "settings-unavailable"
    ];

    public static TheoryData<string> SampleNames => new(s_sampleNames);

    private static TrayViewState Sample(string name) => name switch
    {
        "typical" => TrayPanelSamples.Typical(),
        "empty" => TrayPanelSamples.Empty(),
        "connecting" => TrayPanelSamples.Connecting(),
        "errors" => TrayPanelSamples.Errors(),
        "disconnected" => TrayPanelSamples.Disconnected(),
        "many" => TrayPanelSamples.Many(),
        _ => TrayPanelSamples.Typical()
    };

    /// <summary>Creates a panel for a sample, driving it into the sample's interactive state.</summary>
    private static TrayPanel CreatePanel(string name, TrayPanelPalette palette, double maxHeight = TrayPanel.DefaultMaxHeight)
    {
        var state = Sample(name);
        var panel = new TrayPanel(state, palette) { MaxHeight = maxHeight };
        switch (name)
        {
            case "confirm-stop":
                Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[1]).HitBounds);
                break;
            case "confirm-clear":
                Click(panel, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.ClearRecent).HitBounds);
                break;
            case "confirm-missing":
                panel.ConfirmRemoveMissing(state.RecentAppHosts[1]);
                break;
            case "settings":
                panel.ShowSettings(new(new TrayStartupState(true, true, null), null, Preview: false));
                break;
            case "settings-unavailable":
                panel.ShowSettings(new(new TrayStartupState(false, false, null), null, Preview: false));
                break;
        }
        return panel;
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void LayoutKeepsEveryTargetInsideThePanelAndFooterAtTheBottom(string name)
    {
        var panel = CreatePanel(name, TrayPanelPalette.Light, maxHeight: 600);
        var scene = panel.Scene;
        var bounds = new PanelRect(0, 0, scene.Width, scene.Height);

        Assert.Equal(TrayPanel.Width, scene.Width);
        Assert.InRange(scene.Height, TrayPanel.HeaderHeight + TrayPanel.FooterHeight, 600);
        foreach (var target in scene.Targets)
        {
            Assert.True(bounds.Contains(target.HitBounds) || target.HitBounds.IsEmpty, $"{target.Key} at {target.HitBounds}");
            Assert.True(target.HitBounds.IsEmpty || target.Bounds.Contains(target.HitBounds));
        }
        var footer = scene.Targets.Where(target => target.Kind is PanelTargetKind.Documentation or PanelTargetKind.Settings or PanelTargetKind.Quit).ToList();
        Assert.Equal(3, footer.Count);
        Assert.All(footer, target => Assert.InRange(target.Bounds.Y, scene.Height - TrayPanel.FooterHeight, scene.Height - target.Bounds.Height));
        Assert.All(scene.Targets.Where(target => target.Kind is not (PanelTargetKind.Documentation or PanelTargetKind.Settings
                or PanelTargetKind.Quit or PanelTargetKind.Back)),
            target => Assert.True(target.HitBounds.IsEmpty || panel.Viewport.Contains(target.HitBounds)));
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void SiblingTargetsDoNotOverlapAndButtonsSitInsideTheirRow(string name)
    {
        var scene = CreatePanel(name, TrayPanelPalette.Dark).Scene;
        var rows = scene.Targets.Where(target => target.Kind is PanelTargetKind.Row).ToList();
        var buttons = scene.Targets.Where(target => target.Kind is PanelTargetKind.Dashboard or PanelTargetKind.Stop
            or PanelTargetKind.Start or PanelTargetKind.More).ToList();

        AssertNoOverlap(rows);
        AssertNoOverlap(buttons);
        foreach (var button in buttons)
        {
            var row = Assert.Single(rows, row => row.Key.Host == button.Key.Host && row.Key.Section == button.Key.Section);
            Assert.True(row.Bounds.Contains(button.Bounds), $"{button.Key} outside its row");
        }
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void RowTextNeverRunsUnderTheReservedButtons(string name)
    {
        var scene = CreatePanel(name, TrayPanelPalette.Light).Scene;
        var buttons = scene.Targets.Where(target => target.Kind is PanelTargetKind.Dashboard or PanelTargetKind.Stop
            or PanelTargetKind.Start or PanelTargetKind.More).Select(target => target.Bounds).ToList();
        var texts = scene.Commands.OfType<TextCommand>().ToList();

        Assert.All(texts, text => Assert.DoesNotContain(buttons, button => button.IntersectsWith(text.Bounds)));
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void ClipsAreBalanced(string name)
    {
        var commands = CreatePanel(name, TrayPanelPalette.Light).Scene.Commands;

        Assert.Equal(commands.Count(command => command is PushClipCommand), commands.Count(command => command is PopClipCommand));
    }

    [Fact]
    public void SectionsSplitRunningPinnedAndRecentHosts()
    {
        var scene = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light).Scene;
        var rows = scene.Targets.Where(target => target.Kind == PanelTargetKind.Row).ToList();

        Assert.Equal([PanelSection.Running, PanelSection.Running, PanelSection.Running, PanelSection.Pinned],
            rows.Select(row => row.Key.Section));
        Assert.Contains(scene.Commands.OfType<TextCommand>(), text => text.Text == "RECENT · 2");
        Assert.Contains(scene.Targets, target => target.Kind == PanelTargetKind.ClearRecent);
    }

    [Fact]
    public void RecentListIsCollapsedWhenAppHostsAreListedAndExpandsOnToggle()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);
        Assert.False(panel.IsRecentExpanded);
        var toggle = Assert.Single(panel.Scene.Targets, target => target.Kind == PanelTargetKind.ToggleRecent);

        var response = Click(panel, toggle.HitBounds);

        Assert.Equal(PanelActionKind.ToggleRecent, response.Action?.Kind);
        Assert.True(panel.IsRecentExpanded);
        Assert.Equal(2, panel.Scene.Targets.Count(target => target.Kind == PanelTargetKind.Row && target.Key.Section == PanelSection.Recent));
    }

    [Fact]
    public void RecentListIsExpandedWhenNothingElseIsListed()
    {
        var panel = new TrayPanel(TrayPanelSamples.Empty(), TrayPanelPalette.Light);

        Assert.True(panel.IsRecentExpanded);
        Assert.Contains(panel.Scene.Commands.OfType<TextCommand>(), text => text.Text == "No AppHosts running");
        Assert.Equal(2, panel.Scene.Targets.Count(target => target.Kind == PanelTargetKind.Row));
    }

    [Fact]
    public void ClickingARowOpensItsDashboard()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var row = panel.Scene.Targets.First(target => target.Kind == PanelTargetKind.Row);

        var response = Click(panel, row.HitBounds, offsetX: 10);

        Assert.Equal(PanelActionKind.OpenDashboard, response.Action?.Kind);
        Assert.Equal(state.AppHosts[0], response.Action?.Host);
    }

    [Fact]
    public void ClickingARowWithoutDashboardOpensItsMoreMenuAnchoredAtTheMoreButton()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var host = state.AppHosts[2];
        var row = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.Row && target.Host == host);
        var more = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.More && target.Host == host);

        var response = Click(panel, row.HitBounds, offsetX: 10);

        Assert.Equal(new PanelAction(PanelActionKind.More, host, more.Bounds), response.Action);
    }

    [Fact]
    public void RowButtonsReturnTheirActions()
    {
        var state = TrayPanelSamples.Typical() with { ConfirmStop = false };
        var panel = new TrayPanel(state, TrayPanelPalette.Light);

        Assert.Equal(PanelActionKind.Stop, Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds).Action?.Kind);
        Assert.Equal(PanelActionKind.OpenDashboard, Click(panel, Target(panel, PanelTargetKind.Dashboard, state.AppHosts[0]).HitBounds).Action?.Kind);
        var start = Click(panel, Target(panel, PanelTargetKind.Start, state.AppHosts[3]).HitBounds).Action;
        Assert.Equal(PanelActionKind.Start, start?.Kind);
        Assert.Equal(state.AppHosts[3].Id.AppHostPath, start?.Host?.Id.AppHostPath);
        Assert.Equal(PanelActionKind.Settings, Click(panel, panel.Scene.Targets.Single(t => t.Kind == PanelTargetKind.Settings).HitBounds).Action?.Kind);
        Assert.Equal(PanelActionKind.Quit, Click(panel, panel.Scene.Targets.Single(t => t.Kind == PanelTargetKind.Quit).HitBounds).Action?.Kind);
        Assert.Equal(PanelActionKind.Documentation, Click(panel, panel.Scene.Targets.Single(t => t.Kind == PanelTargetKind.Documentation).HitBounds).Action?.Kind);
    }

    [Fact]
    public void DisabledButtonsDoNothing()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var dashboard = Target(panel, PanelTargetKind.Dashboard, state.AppHosts[2]);

        Assert.False(dashboard.Enabled);
        Assert.Null(Click(panel, dashboard.HitBounds).Action);
    }

    [Fact]
    public void ReleasingOutsideThePressedTargetCancelsTheClick()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var stop = Target(panel, PanelTargetKind.Stop, state.AppHosts[0]);
        var more = Target(panel, PanelTargetKind.More, state.AppHosts[0]);

        panel.PointerDown(stop.HitBounds.CenterX, stop.HitBounds.CenterY);
        var response = panel.PointerUp(more.HitBounds.CenterX, more.HitBounds.CenterY);

        Assert.Null(response.Action);
    }

    [Fact]
    public void StateUpdatesAreDeferredWhileAPressIsInProgress()
    {
        var state = TrayPanelSamples.Typical() with { ConfirmStop = false };
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var stop = Target(panel, PanelTargetKind.Stop, state.AppHosts[0]);

        panel.PointerDown(stop.HitBounds.CenterX, stop.HitBounds.CenterY);
        Assert.False(panel.Update(TrayPanelSamples.Empty()));
        Assert.Same(state, panel.State);
        var response = panel.PointerUp(stop.HitBounds.CenterX, stop.HitBounds.CenterY);

        Assert.Equal(PanelActionKind.Stop, response.Action?.Kind);
        Assert.Equal(state.AppHosts[0], response.Action?.Host);
        Assert.Empty(panel.State.AppHosts);
    }

    [Fact]
    public void RowButtonsAreDrawnOnlyWhileTheRowIsHovered()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var before = panel.Scene.Commands.Count;
        var row = Target(panel, PanelTargetKind.Row, state.AppHosts[0]);

        Assert.True(panel.PointerMoved(row.HitBounds.X + 10, row.HitBounds.CenterY).Redraw);
        var hovered = panel.Scene.Commands.Count;
        Assert.False(panel.PointerMoved(row.HitBounds.X + 11, row.HitBounds.CenterY).Redraw);
        Assert.True(panel.PointerLeft().Redraw);

        // One hover fill plus the dashboard (two strokes), stop (one fill) and more (three fills) icons.
        Assert.Equal(before + 7, hovered);
        Assert.Equal(before, panel.Scene.Commands.Count);
    }

    [Fact]
    public void ManyRowsScrollInsideAFixedHeaderAndFooter()
    {
        var panel = new TrayPanel(TrayPanelSamples.Many(), TrayPanelPalette.Light) { MaxHeight = 480 };
        var scene = panel.Scene;
        var footer = scene.Targets.Single(target => target.Kind == PanelTargetKind.Quit).Bounds;

        Assert.Equal(480, scene.Height);
        Assert.Equal(0, panel.ScrollOffset);
        Assert.True(panel.Scroll(-100).Redraw);
        Assert.Equal(100, panel.ScrollOffset);
        Assert.Equal(footer, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.Quit).Bounds);
        Assert.True(panel.Scroll(-100_000).Redraw);
        var max = panel.ScrollOffset;
        Assert.False(panel.Scroll(-1).Redraw);
        Assert.Equal(max, panel.ScrollOffset);
        var last = panel.Scene.Targets.Last(target => target.Kind == PanelTargetKind.ToggleRecent);
        Assert.InRange(last.Bounds.Bottom, panel.Viewport.Y, panel.Viewport.Bottom);
        Assert.True(panel.Scroll(100_000).Redraw);
        Assert.Equal(0, panel.ScrollOffset);
    }

    [Fact]
    public void RowsScrolledOutOfViewCannotBeClicked()
    {
        var state = TrayPanelSamples.Many();
        var panel = new TrayPanel(state, TrayPanelPalette.Light) { MaxHeight = 400 };
        panel.Scroll(-200);
        var first = panel.Scene.Targets.FirstOrDefault(target => target.Kind == PanelTargetKind.Row && target.Host == state.AppHosts[0]);

        Assert.NotNull(first);
        Assert.True(first.HitBounds.IsEmpty);
        Assert.Null(panel.HitTest(100, TrayPanel.HeaderHeight - 1));
    }

    [Fact]
    public void KeyboardNavigationMovesAcrossRowsAndScrollsTheFocusIntoView()
    {
        var state = TrayPanelSamples.Many();
        var panel = new TrayPanel(state, TrayPanelPalette.Light) { MaxHeight = 400 };

        panel.Key(PanelKey.Down);
        Assert.Equal(new PanelTargetKey(PanelTargetKind.Row, PanelSection.Running, state.AppHosts[0].Id), panel.FocusedTarget);
        for (var i = 1; i < state.AppHosts.Count; i++)
        {
            panel.Key(PanelKey.Down);
        }

        Assert.Equal(new PanelTargetKey(PanelTargetKind.Row, PanelSection.Running, state.AppHosts[^1].Id), panel.FocusedTarget);
        var focused = panel.Scene.Targets.Single(target => target.Key == panel.FocusedTarget);
        Assert.True(panel.Viewport.Contains(focused.Bounds));
        Assert.True(panel.ScrollOffset > 0);
        Assert.Contains(panel.Scene.Commands.OfType<StrokePathCommand>(), stroke => stroke.Color == TrayPanelPalette.Light.Focus);

        var response = panel.Key(PanelKey.Enter);
        Assert.Equal(new PanelAction(PanelActionKind.OpenDashboard, state.AppHosts[^1], focused.Bounds), response.Action);
    }

    [Fact]
    public void TabVisitsInlineButtonsAndSkipsDisabledOnes()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var visited = new List<PanelTargetKey>();
        var enabled = panel.Scene.Targets.Count(target => target.Enabled);
        for (var i = 0; i < enabled; i++)
        {
            panel.Key(PanelKey.Tab);
            visited.Add(panel.FocusedTarget!.Value);
        }

        Assert.Equal(panel.Scene.Targets.Where(target => target.Enabled).Select(target => target.Key), visited);
        Assert.DoesNotContain(new PanelTargetKey(PanelTargetKind.Dashboard, PanelSection.Running, state.AppHosts[2].Id), visited);
        panel.Key(PanelKey.Tab);
        Assert.Equal(visited[0], panel.FocusedTarget);
        panel.Key(PanelKey.ShiftTab);
        Assert.Equal(visited[^1], panel.FocusedTarget);
    }

    [Fact]
    public void EscapeClosesThePanel()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);

        Assert.Equal(PanelActionKind.Close, panel.Key(PanelKey.Escape).Action?.Kind);
    }

    [Fact]
    public void StopReplacesTheRowWithAnInlineConfirmationFocusedOnCancel()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var host = state.AppHosts[1];

        var response = Click(panel, Target(panel, PanelTargetKind.Stop, host).HitBounds);

        Assert.True(response.Redraw);
        Assert.Null(response.Action);
        Assert.Equal(PanelConfirmKind.Stop, panel.Confirmation);
        var scene = panel.Scene;
        Assert.DoesNotContain(scene.Targets, target => target.Kind == PanelTargetKind.Row && target.Host == host);
        Assert.Equal(
            [PanelTargetKind.ConfirmDontAsk, PanelTargetKind.ConfirmCancel, PanelTargetKind.ConfirmAccept],
            scene.Targets.Where(target => target.Host == host).Select(target => target.Kind));
        Assert.Contains(scene.Commands.OfType<TextCommand>(), text => text.Text == $"Stop {host.DisplayName}?");
        Assert.Equal(new PanelTargetKey(PanelTargetKind.ConfirmCancel, PanelSection.Running, host.Id), panel.FocusedTarget);
        Assert.Null(panel.Key(PanelKey.Enter).Action);
        Assert.Null(panel.Confirmation);
        Assert.Equal(new PanelTargetKey(PanelTargetKind.Stop, PanelSection.Running, host.Id), panel.FocusedTarget);
    }

    [Fact]
    public void ConfirmingStopReturnsStopWithTheDontAskAgainChoice()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Dark);
        var host = state.AppHosts[0];
        Click(panel, Target(panel, PanelTargetKind.Stop, host).HitBounds);

        Click(panel, Target(panel, PanelTargetKind.ConfirmDontAsk, host).HitBounds);
        Assert.True(panel.ConfirmationDontAskAgain);
        var action = Click(panel, Target(panel, PanelTargetKind.ConfirmAccept, host).HitBounds).Action;

        Assert.Equal(PanelActionKind.Stop, action?.Kind);
        Assert.Equal(host, action?.Host);
        Assert.True(action?.DontAskAgain);
        Assert.Null(panel.Confirmation);
    }

    [Fact]
    public void StopIsImmediateWhenConfirmationIsOff()
    {
        var state = TrayPanelSamples.Typical() with { ConfirmStop = false };
        var panel = new TrayPanel(state, TrayPanelPalette.Light);

        var action = Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds).Action;

        Assert.Equal(PanelActionKind.Stop, action?.Kind);
        Assert.False(action?.DontAskAgain);
        Assert.Null(panel.Confirmation);
    }

    [Fact]
    public void EscapeCancelsAConfirmationBeforeClosingThePanel()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds);

        var first = panel.Key(PanelKey.Escape);

        Assert.True(first.Redraw);
        Assert.Null(first.Action);
        Assert.Null(panel.Confirmation);
        Assert.Equal(PanelActionKind.Close, panel.Key(PanelKey.Escape).Action?.Kind);
    }

    [Fact]
    public void CancelButtonDismissesTheConfirmation()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds);

        var response = Click(panel, Target(panel, PanelTargetKind.ConfirmCancel, state.AppHosts[0]).HitBounds);

        Assert.Null(response.Action);
        Assert.Null(panel.Confirmation);
        Assert.Contains(panel.Scene.Targets, target => target.Kind == PanelTargetKind.Row && target.Host == state.AppHosts[0]);
    }

    [Fact]
    public void ActingOnAnotherTargetAbandonsTheConfirmation()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds);

        var action = Click(panel, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.Documentation).HitBounds).Action;

        Assert.Equal(PanelActionKind.Documentation, action?.Kind);
        Assert.Null(panel.Confirmation);
    }

    [Fact]
    public void ConfirmationIsDroppedWhenItsAppHostGoesAway()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds);

        panel.Update(state with { AppHosts = state.AppHosts.Skip(1).ToArray() });
        _ = panel.Scene;

        Assert.Null(panel.Confirmation);
    }

    [Fact]
    public void ClearRecentAsksInlineAndReturnsClearRecentWhenConfirmed()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);

        var response = Click(panel, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.ClearRecent).HitBounds);

        Assert.Null(response.Action);
        Assert.Equal(PanelConfirmKind.ClearRecent, panel.Confirmation);
        var texts = panel.Scene.Commands.OfType<TextCommand>().Select(text => text.Text).ToList();
        Assert.Contains("Clear recent AppHosts?", texts);
        Assert.Contains("Pinned AppHosts are kept.", texts);
        Assert.DoesNotContain(panel.Scene.Targets, target => target.Kind is PanelTargetKind.ToggleRecent or PanelTargetKind.ConfirmDontAsk);
        Assert.Equal(new PanelTargetKey(PanelTargetKind.ConfirmCancel, PanelSection.Recent, null), panel.FocusedTarget);
        var accept = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.ConfirmAccept);
        Assert.Equal(PanelActionKind.ClearRecent, Click(panel, accept.HitBounds).Action?.Kind);
        Assert.Null(panel.Confirmation);
    }

    [Fact]
    public void MissingRecentAppHostIsOfferedForRemovalInline()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        var missing = state.RecentAppHosts[1];

        panel.ConfirmRemoveMissing(missing);

        Assert.True(panel.IsRecentExpanded);
        Assert.Equal(PanelConfirmKind.RemoveMissing, panel.Confirmation);
        Assert.Contains(panel.Scene.Commands.OfType<TextCommand>(), text => text.Text == missing.Id.AppHostPath);
        var action = Click(panel, Target(panel, PanelTargetKind.ConfirmAccept, missing).HitBounds).Action;
        Assert.Equal(PanelActionKind.RemoveRecent, action?.Kind);
        Assert.Equal(missing, action?.Host);
        Assert.True(action?.Recent);
    }

    [Fact]
    public void SettingsViewShowsTogglesAndReturnsTheirNewValues()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);
        Assert.Equal(PanelActionKind.Settings, Click(panel, panel.Scene.Targets.Single(t => t.Kind == PanelTargetKind.Settings).HitBounds).Action?.Kind);
        Assert.False(panel.IsSettingsOpen);

        panel.ShowSettings(new(new TrayStartupState(false, true, null), null, Preview: false));

        Assert.True(panel.IsSettingsOpen);
        Assert.DoesNotContain(panel.Scene.Targets, target => target.Kind == PanelTargetKind.Row);
        Assert.Equal(new PanelTargetKey(PanelTargetKind.Back, PanelSection.None, null), panel.FocusedTarget);
        var launch = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn);
        Assert.Equal("Launch at sign-in, off", launch.AccessibleName);
        var setLaunch = Click(panel, launch.HitBounds).Action;
        Assert.Equal(PanelActionKind.SetLaunchAtSignIn, setLaunch?.Kind);
        Assert.True(setLaunch?.Enabled);
        var setConfirm = Click(panel, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.ConfirmStopSetting).HitBounds).Action;
        Assert.Equal(PanelActionKind.SetConfirmStop, setConfirm?.Kind);
        Assert.False(setConfirm?.Enabled);
        var texts = panel.Scene.Commands.OfType<TextCommand>().Select(text => text.Text).ToList();
        Assert.Contains(TraySettingsText.GetAboutDescription(preview: false), texts);
        Assert.Contains(texts, text => text.StartsWith("Version: ", StringComparison.Ordinal));

        panel.UpdateSettings(new(new TrayStartupState(true, true, null), null, Preview: false));
        Assert.Equal("Launch at sign-in, on", panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn).AccessibleName);
    }

    [Fact]
    public void LaunchAtSignInCannotBeTurnedOnWithoutAStableInstallation()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);
        panel.ShowSettings(new(new TrayStartupState(false, false, null), null, Preview: false));

        var launch = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn);

        Assert.False(launch.Enabled);
        Assert.Null(Click(panel, launch.HitBounds).Action);
        Assert.Contains(panel.Scene.Commands.OfType<TextCommand>(), text => text.Text == TraySettingsText.StableNativeInstallationRequired);
    }

    [Fact]
    public void SettingsErrorsAreShownUnderTheToggle()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);
        panel.ShowSettings(new(null, "Could not read launch at sign-in: denied", Preview: false));

        var launch = panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.LaunchAtSignIn);

        Assert.False(launch.Enabled);
        Assert.Contains(panel.Scene.Commands.OfType<TextCommand>(),
            text => text.Text == "Could not read launch at sign-in: denied" && text.Color == TrayPanelPalette.Light.Danger);
    }

    [Fact]
    public void BackAndEscapeLeaveSettingsAndRestoreTheList()
    {
        var panel = new TrayPanel(TrayPanelSamples.Many(), TrayPanelPalette.Light) { MaxHeight = 400 };
        panel.Scroll(-120);
        var scroll = panel.ScrollOffset;
        panel.ShowSettings(new(new TrayStartupState(false, true, null), null, Preview: false));
        Assert.Equal(0, panel.ScrollOffset);

        var escape = panel.Key(PanelKey.Escape);

        Assert.Null(escape.Action);
        Assert.False(panel.IsSettingsOpen);
        Assert.Equal(scroll, panel.ScrollOffset);
        Assert.Equal(new PanelTargetKey(PanelTargetKind.Settings, PanelSection.None, null), panel.FocusedTarget);

        panel.ShowSettings(new(new TrayStartupState(false, true, null), null, Preview: false));
        Assert.Null(Click(panel, panel.Scene.Targets.Single(target => target.Kind == PanelTargetKind.Back).HitBounds).Action);
        Assert.False(panel.IsSettingsOpen);
    }

    [Fact]
    public void ResetLeavesConfirmationsAndSettings()
    {
        var state = TrayPanelSamples.Typical();
        var panel = new TrayPanel(state, TrayPanelPalette.Light);
        Click(panel, Target(panel, PanelTargetKind.Stop, state.AppHosts[0]).HitBounds);
        panel.Reset();
        Assert.Null(panel.Confirmation);

        panel.ShowSettings(new(null, null, Preview: false));
        panel.Reset();
        Assert.False(panel.IsSettingsOpen);
    }

    [Fact]
    public void ToggleGlyphStaysInsideItsBox()
    {
        foreach (var on in new[] { true, false })
        {
            var commands = new List<PanelCommand>();
            var box = new PanelRect(10, 20, 32, 18);
            TrayPanelGlyphs.AddToggle(commands, on, enabled: true, box, TrayPanelPalette.Light);

            Assert.All(commands, command => Assert.True(box.Inset(-0.5, -0.5).Contains(GetBounds(command)), $"{command}"));
        }
    }

    [Fact]
    public void FocusOnARemovedRowIsForgotten()
    {
        var panel = new TrayPanel(TrayPanelSamples.Typical(), TrayPanelPalette.Light);
        panel.Key(PanelKey.Down);
        Assert.NotNull(panel.FocusedTarget);

        panel.Update(TrayPanelSamples.Connecting());
        _ = panel.Scene;

        Assert.Null(panel.FocusedTarget);
    }

    [Fact]
    public void ResetClearsInteractionState()
    {
        var panel = new TrayPanel(TrayPanelSamples.Many(), TrayPanelPalette.Light) { MaxHeight = 400 };
        panel.Key(PanelKey.End);
        panel.Scroll(-50);

        panel.Reset();

        Assert.Null(panel.FocusedTarget);
        Assert.Equal(0, panel.ScrollOffset);
    }

    [Theory]
    [InlineData((int)DiscoveryState.Connecting, "Connecting…")]
    [InlineData((int)DiscoveryState.Disconnected, "Reconnecting…")]
    [InlineData((int)DiscoveryState.Incompatible, "Incompatible CLI")]
    [InlineData((int)DiscoveryState.LimitExceeded, "Discovery limit exceeded")]
    public void HeaderStatusReportsDiscoveryProblems(int discovery, string expected)
    {
        var (text, _) = TrayPanel.GetHeaderStatus(new((DiscoveryState)discovery, [], ""), TrayPanelPalette.Light);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void HeaderStatusSummarizesRunningAndAttention()
    {
        Assert.Equal("3 running · 1 needs attention", TrayPanel.GetHeaderStatus(TrayPanelSamples.Typical(), TrayPanelPalette.Light).Text);
        Assert.Equal("No AppHosts running", TrayPanel.GetHeaderStatus(TrayPanelSamples.Empty(), TrayPanelPalette.Light).Text);
        Assert.Equal("3 running · 1 starting · 2 need attention",
            TrayPanel.GetHeaderStatus(TrayPanelSamples.Errors(), TrayPanelPalette.Light).Text);
    }

    [Fact]
    public void RowDetailShowsStatusLocationAndPidOnlyForDuplicateNames()
    {
        var host = TrayPanelSamples.Running("/Users/dev/src/eShop/src/eShop.AppHost/eShop.AppHost.csproj", 48213, AppHostHealth.Warning);

        Assert.Equal(("Needs attention · src/eShop.AppHost", false), TrayPanel.GetRowDetail(host, live: true, showPid: false));
        Assert.Equal(("Needs attention · src/eShop.AppHost · PID 48213", false), TrayPanel.GetRowDetail(host, live: true, showPid: true));
        Assert.Equal(("Status unavailable · src/eShop.AppHost", false), TrayPanel.GetRowDetail(host, live: false, showPid: false));
        Assert.Equal(("Boom.", true), TrayPanel.GetRowDetail(host with { Error = "Boom." }, live: true, showPid: false));

        var scene = new TrayPanel(TrayPanelSamples.Errors(), TrayPanelPalette.Light).Scene;
        Assert.Contains(scene.Commands.OfType<TextCommand>(), text => text.Text == "Unhealthy · src/eShop.AppHost · PID 48213");
    }

    [Theory]
    [InlineData(true, false, false, (int)AppHostHealth.Healthy, (int)PanelStatusGlyph.Healthy)]
    [InlineData(true, false, false, (int)AppHostHealth.Warning, (int)PanelStatusGlyph.Warning)]
    [InlineData(true, false, false, (int)AppHostHealth.Unhealthy, (int)PanelStatusGlyph.Unhealthy)]
    [InlineData(true, false, false, (int)AppHostHealth.Unknown, (int)PanelStatusGlyph.Unknown)]
    [InlineData(false, false, false, (int)AppHostHealth.Healthy, (int)PanelStatusGlyph.Unknown)]
    [InlineData(true, true, false, (int)AppHostHealth.Healthy, (int)PanelStatusGlyph.Busy)]
    [InlineData(true, false, true, (int)AppHostHealth.Healthy, (int)PanelStatusGlyph.Warning)]
    public void StatusGlyphFollowsHealthAndActivity(bool live, bool stopping, bool error, int health, int expected)
    {
        var host = TrayPanelSamples.Running("/src/A/A.AppHost.csproj", 1, (AppHostHealth)health, stopping: stopping, error: error ? "Failed." : null);

        Assert.Equal((PanelStatusGlyph)expected, TrayPanel.GetStatusGlyph(host, live));
    }

    [Fact]
    public void OfflineRowsUseStoppedOrBusyGlyphs()
    {
        Assert.Equal(PanelStatusGlyph.Stopped, TrayPanel.GetStatusGlyph(TrayPanelSamples.Offline("/src/A/A.cs"), live: true));
        Assert.Equal(PanelStatusGlyph.Busy, TrayPanel.GetStatusGlyph(TrayPanelSamples.Offline("/src/A/A.cs", starting: true), live: true));
        Assert.Equal(PanelStatusGlyph.Warning, TrayPanel.GetStatusGlyph(TrayPanelSamples.Offline("/src/A/A.cs", error: "No."), live: true));
    }

    [Fact]
    public void NoticeShowsABannerAndPushesTheListDown()
    {
        var withNotice = new TrayPanel(TrayPanelSamples.Errors(), TrayPanelPalette.Light);
        var without = new TrayPanel(TrayPanelSamples.Errors() with { Notice = null }, TrayPanelPalette.Light);

        Assert.Contains(withNotice.Scene.Commands.OfType<TextCommand>(), text => text.Text.StartsWith("Unable to save", StringComparison.Ordinal));
        Assert.True(withNotice.Viewport.Y > without.Viewport.Y);
        Assert.Equal(TrayPanel.HeaderHeight, without.Viewport.Y);
    }

    [Theory]
    [InlineData(0, 90)]
    [InlineData(-60, 300)]
    [InlineData(45, -200)]
    [InlineData(0, 360)]
    public void ArcsUseSegmentsOfAtMostNinetyDegreesThatStayOnTheCircle(double start, double sweep)
    {
        var path = new PanelPath().Arc(10, 20, 5, start, sweep);
        var cubics = path.Segments.Where(segment => segment.Verb == PanelPathVerb.CubicTo).ToList();

        Assert.Equal((int)Math.Ceiling(Math.Abs(sweep) / 90), cubics.Count);
        foreach (var cubic in cubics)
        {
            Assert.Equal(5, Math.Sqrt(Math.Pow(cubic.X3 - 10, 2) + Math.Pow(cubic.Y3 - 20, 2)), 6);
            // The curve midpoint of a 4/3 tan(φ/4) approximation is within 0.03% of the radius.
            var mid = Midpoint(path.Segments[path.Segments.ToList().IndexOf(cubic) - 1], cubic);
            Assert.Equal(5, Math.Sqrt(Math.Pow(mid.X - 10, 2) + Math.Pow(mid.Y - 20, 2)), 2);
        }
        var end = (start + sweep) * Math.PI / 180;
        Assert.Equal(10 + 5 * Math.Cos(end), cubics[^1].X3, 6);
        Assert.Equal(20 + 5 * Math.Sin(end), cubics[^1].Y3, 6);
    }

    [Fact]
    public void QuarterArcControlPointsMatchTheStandardKappa()
    {
        var cubic = new PanelPath().Arc(0, 0, 1, 0, 90).Segments[1];

        Assert.Equal(PanelPath.QuarterArcKappa, cubic.Y1, 6);
        Assert.Equal(PanelPath.QuarterArcKappa, cubic.X2, 6);
    }

    [Fact]
    public void RoundedRectStaysInsideItsBounds()
    {
        var rect = new PanelRect(8, 10, 100, 40);
        var path = PanelPath.RoundedRect(rect, 6);

        Assert.Equal(rect, path.GetControlBounds());
        Assert.Equal(PanelPathVerb.Close, path.Segments[^1].Verb);
    }

    [Theory]
    [InlineData((int)PanelStatusGlyph.Healthy)]
    [InlineData((int)PanelStatusGlyph.Warning)]
    [InlineData((int)PanelStatusGlyph.Unhealthy)]
    [InlineData((int)PanelStatusGlyph.Unknown)]
    [InlineData((int)PanelStatusGlyph.Stopped)]
    [InlineData((int)PanelStatusGlyph.Busy)]
    public void StatusGlyphsStayInsideTheirBox(int value)
    {
        var glyph = (PanelStatusGlyph)value;
        var commands = new List<PanelCommand>();
        var box = new PanelRect(40, 60, 20, 20);
        TrayPanelGlyphs.AddStatus(commands, glyph, box, TrayPanelPalette.Light);

        Assert.NotEmpty(commands);
        Assert.All(commands, command => Assert.True(box.Contains(GetBounds(command)), $"{glyph}: {GetBounds(command)}"));
    }

    [Theory]
    [InlineData((int)PanelIcon.ExternalLink)]
    [InlineData((int)PanelIcon.Stop)]
    [InlineData((int)PanelIcon.Play)]
    [InlineData((int)PanelIcon.More)]
    [InlineData((int)PanelIcon.Gear)]
    [InlineData((int)PanelIcon.Power)]
    [InlineData((int)PanelIcon.Book)]
    [InlineData((int)PanelIcon.ChevronDown)]
    [InlineData((int)PanelIcon.ChevronRight)]
    [InlineData((int)PanelIcon.Alert)]
    [InlineData((int)PanelIcon.Layers)]
    public void IconsStayInsideTheirBox(int value)
    {
        var icon = (PanelIcon)value;
        var commands = new List<PanelCommand>();
        var box = new PanelRect(4, 8, 16, 16);
        TrayPanelGlyphs.AddIcon(commands, icon, box, TrayPanelPalette.Light.TextPrimary);

        Assert.NotEmpty(commands);
        Assert.All(commands, command => Assert.True(box.Contains(GetBounds(command)), $"{icon}: {GetBounds(command)}"));
    }

    [Fact]
    public void PalettesParseTheSpecifiedColors()
    {
        Assert.Equal(new PanelColor(0xF9, 0xF9, 0xFB), TrayPanelPalette.Light.Background);
        Assert.Equal(new PanelColor(0x6A, 0x4B, 0xD6), TrayPanelPalette.Light.Accent);
        Assert.Equal(new PanelColor(0, 0, 0, 20), TrayPanelPalette.Light.Separator);
        Assert.Equal(new PanelColor(0x25, 0x25, 0x29), TrayPanelPalette.Dark.Background);
        Assert.Equal(new PanelColor(255, 255, 255, 18), TrayPanelPalette.Dark.Hover);
        Assert.Equal("#11223344", PanelColor.FromHex("#11223344").ToHex());
        Assert.Equal(new PanelColor(128, 128, 128), new PanelColor(255, 255, 255, 128).Over(new(0, 0, 0)));
    }

    [Fact]
    public void HighContrastPaletteUsesSystemColors()
    {
        var palette = TrayPanelPalette.FromSystemColors(new(0, 0, 0), new(255, 255, 255), new(26, 235, 255), new(63, 242, 63));
        var scene = new TrayPanel(TrayPanelSamples.Errors(), palette).Scene;

        Assert.True(palette.IsHighContrast);
        Assert.Equal(new PanelColor(255, 255, 255), palette.TextSecondary);
        Assert.All(scene.Commands.OfType<TextCommand>().Where(text => text.Color != palette.Accent),
            text => Assert.Equal(new PanelColor(255, 255, 255), text.Color));
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void SvgRendererProducesWellFormedDocuments(string name)
    {
        var svg = TrayPanelSvgRenderer.Render(CreatePanel(name, TrayPanelPalette.Dark).Scene);
        var document = XDocument.Parse(svg);

        Assert.Equal("svg", document.Root!.Name.LocalName);
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "text");
    }

    /// <summary>
    /// Writes preview images of each state to ASPIRE_TRAY_PANEL_PREVIEW_DIR, or to the test output
    /// directory, for design review and documentation.
    /// </summary>
    [Fact]
    public void WritePreviewImages()
    {
        var directory = Environment.GetEnvironmentVariable("ASPIRE_TRAY_PANEL_PREVIEW_DIR") is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppContext.BaseDirectory, "TrayPanelPreviews");
        Directory.CreateDirectory(directory);
        var logo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "aspire.png"));
        var written = new List<string>();
        foreach (var palette in new[] { TrayPanelPalette.Light, TrayPanelPalette.Dark })
        {
            foreach (var name in s_sampleNames)
            {
                var panel = CreatePanel(name, palette, maxHeight: 520);
                switch (name)
                {
                    case "typical":
                        // Show the hover affordance on the second row.
                        var row = panel.Scene.Targets.Where(target => target.Kind == PanelTargetKind.Row).ElementAt(1);
                        panel.PointerMoved(row.Bounds.X + 40, row.Bounds.CenterY);
                        break;
                    case "many":
                        panel.Scroll(-140);
                        panel.PointerMoved(200, panel.Viewport.Y + 140);
                        break;
                }
                var path = Path.Combine(directory, $"{name}-{palette.Name}.svg");
                File.WriteAllText(path, TrayPanelSvgRenderer.Render(panel.Scene, logo, margin: 24));
                written.Add(path);
            }
        }

        Assert.All(written, path => Assert.True(new FileInfo(path).Length > 0));
    }

    private static PanelHitTarget Target(TrayPanel panel, PanelTargetKind kind, AppHostMenuItem host)
        => panel.Scene.Targets.Single(target => target.Kind == kind && target.Host == host);

    private static PanelResponse Click(TrayPanel panel, PanelRect bounds, double? offsetX = null)
    {
        var x = offsetX is { } dx ? bounds.X + dx : bounds.CenterX;
        panel.PointerMoved(x, bounds.CenterY);
        panel.PointerDown(x, bounds.CenterY);
        return panel.PointerUp(x, bounds.CenterY);
    }

    private static void AssertNoOverlap(List<PanelHitTarget> targets)
    {
        for (var i = 0; i < targets.Count; i++)
        {
            for (var j = i + 1; j < targets.Count; j++)
            {
                Assert.False(targets[i].Bounds.IntersectsWith(targets[j].Bounds), $"{targets[i].Key} overlaps {targets[j].Key}");
            }
        }
    }

    private static PanelRect GetBounds(PanelCommand command)
    {
        var (path, width) = command switch
        {
            FillPathCommand fill => (fill.Path, 0.0),
            StrokePathCommand stroke => (stroke.Path, stroke.Width),
            _ => throw new InvalidOperationException()
        };
        // Control points bound the curve; a stroke extends half its width beyond the path.
        return path.GetControlBounds().Inset(-width / 2, -width / 2);
    }

    private static (double X, double Y) Midpoint(PanelPathSegment previous, PanelPathSegment cubic)
        => (0.125 * previous.X3 + 0.375 * cubic.X1 + 0.375 * cubic.X2 + 0.125 * cubic.X3,
            0.125 * previous.Y3 + 0.375 * cubic.Y1 + 0.375 * cubic.Y2 + 0.125 * cubic.Y3);
}
