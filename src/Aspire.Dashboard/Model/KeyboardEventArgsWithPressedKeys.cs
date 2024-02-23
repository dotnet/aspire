// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Aspire.Dashboard.Model;

public class KeyboardEventArgsWithPressedKeys : KeyboardEventArgs
{
    public string[] CurrentlyHeldKeys { get; set; } = default!;
}
