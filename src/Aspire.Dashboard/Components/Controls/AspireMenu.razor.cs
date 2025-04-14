// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class AspireMenu : FluentComponentBase
{
    private FluentMenu? _menu;

    [Parameter]
    public string? Anchor { get; set; }

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public bool Anchored { get; set; } = true;

    /// <summary>
    /// Raised when the <see cref="Open"/> property changed.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public required IList<MenuButtonItem> Items { get; set; }

    public async Task CloseAsync()
    {
        if (_menu is { } menu)
        {
            await menu.CloseAsync();
        }
    }

    public async Task OpenAsync(int clientX, int clientY)
    {
        if (_menu is { } menu)
        {
            await menu.OpenAsync(clientX, clientY);
        }
    }

    private async Task HandleItemClicked(MenuButtonItem item)
    {
        if (item.OnClick is {} onClick)
        {
            await onClick();
        }
        Open = false;
    }

    private Task OnOpenChanged(bool open)
    {
        Open = open;

        return OpenChanged.HasDelegate
            ? OpenChanged.InvokeAsync(open)
            : Task.CompletedTask;
    }
}
