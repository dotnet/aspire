// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Aspire.Dashboard.Extensions;

internal static class KeyboardEventArgsExtensions
{
    public static bool NoModifiersPressed(this KeyboardEventArgs args)
    {
        return !args.AltKey && !args.CtrlKey && !args.MetaKey && !args.ShiftKey;
    }

    public static bool OnlyShiftPressed(this KeyboardEventArgs args)
    {
        return args.ShiftKey && !args.AltKey && !args.CtrlKey && !args.MetaKey;
    }
}
