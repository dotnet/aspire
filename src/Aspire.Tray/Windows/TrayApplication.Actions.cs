// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Tray;

internal sealed partial class TrayApplication
{
    private void Dispatch(ActionTarget target)
    {
        if (_quitRequested)
        {
            return;
        }
        HideMenuTooltip();
        _dispatchDepth++;
        try
        {
            switch (target.Kind)
            {
                case ActionKind.Dashboard:
                    OpenUrl(controller.GetDashboardUri(target.Id));
                    break;
                case ActionKind.Stop:
                    if (!RequireCurrentRow(target.Id).CanStop)
                    {
                        throw new InvalidOperationException("Stop is no longer available for the selected AppHost.");
                    }
                    var host = controller.RequireLiveInstance(target.Id);
                    if (ConfirmStop(host))
                    {
                        // The controller revalidates the full lifetime after the modal loop.
                        controller.RequestStop(target.Id);
                        // Save only after dispatch accepts the exact instance. A canceled or
                        // stale action must not suppress later warnings.
                        if (_suppressStopConfirmation)
                        {
                            controller.SetConfirmStop(false);
                        }
                    }
                    break;
                case ActionKind.StopConfirmed:
                    if (!RequireCurrentRow(target.Id).CanStop)
                    {
                        throw new InvalidOperationException("Stop is no longer available for the selected AppHost.");
                    }
                    controller.RequestStop(controller.RequireLiveInstance(target.Id).Id);
                    if (target.DontAskAgain)
                    {
                        controller.SetConfirmStop(false);
                    }
                    break;
                case ActionKind.StartFromPanel:
                    // A recent entry whose source file is gone becomes an inline removal prompt
                    // in the panel instead of the dialog the native menu shows.
                    if (_panel is not null && _panelVisible
                        && controller.State.RecentAppHosts.FirstOrDefault(host => host.Id == target.Id) is { } recent
                        && TrayAppHostPath.IsMissing(recent.Id.AppHostPath))
                    {
                        ApplyPanelResponse(_panel.ConfirmRemoveMissing(recent));
                        break;
                    }
                    goto case ActionKind.Start;
                case ActionKind.ClearRecentConfirmed:
                    controller.ClearRecent();
                    break;
                case ActionKind.RemoveRecent:
                    controller.RemoveRecent(target.Id.AppHostPath);
                    break;
                case ActionKind.SetConfirmStop:
                    controller.SetConfirmStop(target.Enabled);
                    break;
                case ActionKind.Start:
                    RequireCurrentRow(target.Id);
                    if (RequireProjectFile(target.Id.AppHostPath))
                    {
                        controller.RequestStart(target.Id.AppHostPath);
                    }
                    break;
                case ActionKind.TogglePin:
                    var row = RequireCurrentRow(target.Id);
                    controller.SetPinned(row.Id.AppHostPath, !row.IsPinned);
                    break;
                case ActionKind.CopyPath:
                    RequireCurrentRow(target.Id);
                    CopyPath(Path.GetDirectoryName(target.Id.AppHostPath)!);
                    break;
                case ActionKind.Explorer:
                case ActionKind.OpenIn:
                    RequireCurrentRow(target.Id);
                    if (RequireProjectFile(target.Id.AppHostPath))
                    {
                        if (smokeSeconds is not null && !_interactiveSmoke)
                        {
                            throw new InvalidOperationException("External application launch is disabled in smoke mode.");
                        }
                        if (target.Kind == ActionKind.Explorer)
                        {
                            FolderApplication.ShowInExplorer(target.Id.AppHostPath);
                        }
                        else
                        {
                            target.Application!.Open(Path.GetDirectoryName(target.Id.AppHostPath)!);
                        }
                    }
                    break;
                case ActionKind.ClearRecent:
                    if (Confirm("Clear recently opened AppHosts?", "Pinned AppHosts are kept. This can't be undone.", "Clear"))
                    {
                        controller.ClearRecent();
                    }
                    break;
                case ActionKind.Documentation:
                    OpenUrl(new Uri("https://aspire.dev"));
                    break;
                case ActionKind.Settings:
                    ShowSettings();
                    break;
                case ActionKind.Quit:
                    RequestQuit();
                    break;
            }
        }
        catch (Exception ex)
        {
            // This boundary covers native commands and their nested modal loops.
            controller.ReportActionError(ex.Message);
            Program.Log($"Tray action failed: {ex.Message}");
            if (smokeSeconds is not null && !_interactiveSmoke)
            {
                if (ErrorForSmoke is null)
                {
                    throw;
                }
                ErrorForSmoke(ex);
            }
            else
            {
                ShowMessage("Aspire", ex.Message, NativeMethods.MbIconError);
            }
        }
        finally
        {
            _dispatchDepth--;
            RequestRefresh();
        }
    }

    private AppHostMenuItem RequireCurrentRow(AppHostId id)
    {
        var state = controller.State;
        return state.AppHosts.Concat(state.RecentAppHosts).SingleOrDefault(host => host.Id == id)
            ?? throw new InvalidOperationException("The selected AppHost is no longer available.");
    }

    private bool RequireProjectFile(string path)
    {
        if (!TrayAppHostPath.IsMissing(path))
        {
            TrayAppHostPath.RequireExistingFile(path);
            return true;
        }
        if (controller.State.AppHosts.Any(host => host.IsPinned && !host.IsRunning && TrayAppHostPath.Comparer.Equals(host.Id.AppHostPath, path)))
        {
            controller.PruneMissingPinnedAppHosts();
        }
        else if (controller.State.RecentAppHosts.Any(host => TrayAppHostPath.Comparer.Equals(host.Id.AppHostPath, path)))
        {
            if (Confirm("AppHost not found", $"This AppHost no longer exists:\n{path}\n\nRemove it from recently opened AppHosts?", "Remove"))
            {
                controller.RemoveRecent(path);
            }
        }
        else
        {
            throw new FileNotFoundException("The selected AppHost source file no longer exists.", path);
        }
        return false;
    }

    private void ShowMessage(string title, string detail, uint flags)
    {
        var previousOwner = _modalOwner;
        _modalOwner = _settingsWindow != 0 ? _settingsWindow : _window;
        _modalDepth++;
        try
        {
            if (smokeSeconds is not null && !_interactiveSmoke)
            {
                // MB_OK reports IDOK as its default, but its sole button uses IDCANCEL
                // so Escape can dismiss it. Sending IDOK leaves the native dialog open.
                _smokeDialog = new(1, 2);
            }
            NativeCallException.Require(NativeMethods.MessageBox(_modalOwner, detail, title, flags) != 0, "MessageBoxW");
        }
        finally
        {
            _smokeDialog = null;
            _modalDepth--;
            _modalOwner = previousOwner;
            RequestRefresh();
        }
    }

    private void OpenUrl(Uri uri)
    {
        if (smokeSeconds is not null)
        {
            (OpenUrlForSmoke ?? throw new InvalidOperationException("Smoke URL handler is missing."))(uri);
            return;
        }
        // ShellExecute receives only a controller-validated http(s) URL, never a command string.
        var result = NativeMethods.ShellExecute(_window, "open", uri.AbsoluteUri, null, null, 1);
        if (result <= 32)
        {
            throw new NativeCallException("ShellExecuteW(open URL)", (int)result);
        }
    }
}
