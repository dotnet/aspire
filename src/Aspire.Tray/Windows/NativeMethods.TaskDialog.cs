// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Tray;

// Task dialog interop from commctrl.h.
// https://learn.microsoft.com/windows/win32/api/commctrl/ns-commctrl-taskdialogconfig
internal static unsafe partial class NativeMethods
{
    internal const uint TdfAllowDialogCancellation = 0x0008;
    internal const uint TdfPositionRelativeToWindow = 0x1000;

    // MAKEINTRESOURCEW(-1): the stock warning icon.
    internal const nint TdWarningIcon = 0xFFFF;

    internal const uint TdnCreated = 0;
    internal const uint TdnDestroyed = 5;
    internal const uint TdnVerificationClicked = 8;

    // WM_USER + 102 and WM_USER + 113.
    internal const uint TdmClickButton = 0x0466;
    internal const uint TdmClickVerification = 0x0471;

    // commctrl.h declares TASKDIALOGCONFIG and TASKDIALOG_BUTTON inside pshpack1.h, so both are
    // byte-packed on every architecture, unlike the other Win32 structs here.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TaskDialogButton
    {
        public int Id;
        public char* Text;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TaskDialogConfig
    {
        public uint Size;
        public nint Parent;
        public nint Instance;
        public uint Flags;
        public uint CommonButtons;
        public char* WindowTitle;
        public nint MainIcon;
        public char* MainInstruction;
        public char* Content;
        public uint ButtonCount;
        public TaskDialogButton* Buttons;
        public int DefaultButton;
        public uint RadioButtonCount;
        public TaskDialogButton* RadioButtons;
        public int DefaultRadioButton;
        public char* VerificationText;
        public char* ExpandedInformation;
        public char* ExpandedControlText;
        public char* CollapsedControlText;
        public nint FooterIcon;
        public char* Footer;
        public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint, int> Callback;
        public nint CallbackData;
        public uint Width;
    }

    [LibraryImport("comctl32.dll")]
    internal static partial int TaskDialogIndirect(TaskDialogConfig* config, int* button, int* radioButton, int* verificationChecked);
}
