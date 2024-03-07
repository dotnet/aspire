// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Utilities;

namespace Aspire.Dashboard.Components;

public partial class AspireAppBarItem : FluentComponentBase
{
    [Parameter, EditorRequired]
    public required string Href { get; set; }

    [Parameter]
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    [Parameter, EditorRequired]
    public required Icon Icon { get; set; }

    [Parameter]
    public Icon? SecondaryIcon { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Tooltip { get; set; }

    internal string? ClassValue => new CssBuilder("appbar-item")
        .AddClass(Class)
        .Build();
}
