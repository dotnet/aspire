// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aspire.Tray;

/// <summary>
/// The confirmations of the native menu path, shown as task dialogs. The flyout panel confirms
/// inline instead and never reaches these.
/// </summary>
internal sealed unsafe partial class TrayApplication
{
    // IDOK and IDCANCEL, so Escape, Alt+F4 and WM_CLOSE (all IDCANCEL with
    // TDF_ALLOW_DIALOG_CANCELLATION) mean Cancel, and the smoke harness keeps answering by ID.
    private const int ConfirmAcceptId = 1;
    private const int ConfirmCancelId = 2;
    private const string StopTitle = "Stop AppHost";
    private const string DontAskAgainText = "Don't ask again";

    private bool _suppressStopConfirmation;
    private TaskDialogRequest? _taskDialogRequest;
    private nint _taskDialog;
    private bool _taskDialogVerificationChecked;

    private bool ConfirmStop(AppHostInfo host)
    {
        _suppressStopConfirmation = false;
        if (!controller.ConfirmStop)
        {
            return true;
        }
        var (accepted, dontAskAgain) = ShowConfirmation(new(StopTitle,
            $"Stop {AppHostPresentation.GetTitle(host)}?",
            $"This stops the AppHost (PID {host.AppHostPid}) and its running resources. Persistent resources are left running.",
            "Stop AppHost", DontAskAgainText, Warning: false));
        _suppressStopConfirmation = accepted && dontAskAgain;
        return accepted;
    }

    private bool Confirm(string instruction, string content, string acceptText)
        => ShowConfirmation(new(instruction, instruction, content, acceptText, VerificationText: null, Warning: true)).Accepted;

    /// <summary>
    /// Shows a modal task dialog with a destructive button and a default Cancel button, and an
    /// optional verification checkbox that starts unchecked.
    /// </summary>
    /// <remarks>
    /// TaskDialogIndirect needs comctl32 version 6, which app.manifest selects.
    /// https://learn.microsoft.com/windows/win32/api/commctrl/nf-commctrl-taskdialogindirect
    /// </remarks>
    private (bool Accepted, bool Verified) ShowConfirmation(TaskDialogRequest request)
    {
        var previousOwner = _modalOwner;
        _modalOwner = _settingsWindow != 0 ? _settingsWindow : _window;
        _modalDepth++;
        _taskDialogRequest = request;
        _taskDialogVerificationChecked = false;
        try
        {
            if (smokeSeconds is not null && !_interactiveSmoke)
            {
                // SafeConfirmation tells the harness the dialog defaults to Cancel, as before.
                var accept = ConfirmForSmoke?.Invoke(request.SmokeTitle, $"{request.Instruction}\n\n{request.Content}", NativeMethods.SafeConfirmation)
                    ?? throw new InvalidOperationException("Smoke confirmation handler is missing.");
                _smokeDialog = new(ConfirmCancelId, accept ? ConfirmAcceptId : ConfirmCancelId);
            }
            int button = 0, verified = 0;
            fixed (char* title = "Aspire")
            fixed (char* instruction = request.Instruction)
            fixed (char* content = request.Content)
            fixed (char* acceptText = request.AcceptText)
            fixed (char* cancelText = "Cancel")
            fixed (char* verification = request.VerificationText)
            {
                var buttons = stackalloc NativeMethods.TaskDialogButton[2];
                buttons[0] = new() { Id = ConfirmAcceptId, Text = acceptText };
                buttons[1] = new() { Id = ConfirmCancelId, Text = cancelText };
                var config = new NativeMethods.TaskDialogConfig
                {
                    Size = (uint)sizeof(NativeMethods.TaskDialogConfig),
                    Parent = _modalOwner,
                    Instance = _module,
                    // Center on Settings when it owns the dialog; otherwise on the monitor, because
                    // the message-only window has no position.
                    Flags = NativeMethods.TdfAllowDialogCancellation
                        | (_modalOwner == _settingsWindow ? NativeMethods.TdfPositionRelativeToWindow : 0),
                    WindowTitle = title,
                    MainIcon = request.Warning ? NativeMethods.TdWarningIcon : 0,
                    MainInstruction = instruction,
                    Content = content,
                    ButtonCount = 2,
                    Buttons = buttons,
                    DefaultButton = ConfirmCancelId,
                    VerificationText = verification,
                    Callback = &TaskDialogCallback
                };
                var result = NativeMethods.TaskDialogIndirect(&config, &button, null, request.VerificationText is null ? null : &verified);
                if (result < 0)
                {
                    // TaskDialogIndirect reports failures as an HRESULT, not through GetLastError.
                    throw new NativeCallException("TaskDialogIndirect", result);
                }
            }
            if (_callbackFailure is not null)
            {
                throw _callbackFailure;
            }
            var accepted = button == ConfirmAcceptId && !_quitRequested;
            return (accepted, accepted && request.VerificationText is not null && verified != 0);
        }
        finally
        {
            _taskDialogRequest = null;
            _taskDialog = 0;
            _smokeDialog = null;
            DialogReadyForSmoke = null;
            _modalDepth--;
            _modalOwner = previousOwner;
            RequestRefresh();
        }
    }

    // TaskDialogCallbackProc notifications.
    // https://learn.microsoft.com/windows/win32/api/commctrl/nc-commctrl-pftaskdialogcallback
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int TaskDialogCallback(nint window, uint notification, nuint wParam, nint lParam, nint data)
    {
        try
        {
            if (s_current is { } application)
            {
                switch (notification)
                {
                    case NativeMethods.TdnCreated:
                        application._taskDialog = window;
                        break;
                    case NativeMethods.TdnVerificationClicked:
                        application._taskDialogVerificationChecked = wParam != 0;
                        break;
                    case NativeMethods.TdnDestroyed:
                        application._taskDialog = 0;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            // Never unwind a managed exception through the Win32 modal loop.
            if (s_current is { } application)
            {
                application._callbackFailure ??= ex;
                Program.Log($"Task dialog callback failed: {ex.Message}");
            }
        }
        return 0;
    }

    /// <summary>One task dialog. <c>SmokeTitle</c> is what the smoke harness sees; "Stop AppHost" selects its stop checks.</summary>
    private sealed record TaskDialogRequest(string SmokeTitle, string Instruction, string Content, string AcceptText,
        string? VerificationText, bool Warning);
}
