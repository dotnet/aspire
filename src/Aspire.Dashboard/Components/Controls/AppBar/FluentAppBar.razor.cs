// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Utilities;

namespace Aspire.Dashboard.Components;

public partial class FluentAppBar : FluentComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal string? ClassValue => new CssBuilder("nav-menu-container")
        .AddClass(Class)
        .Build();
}
