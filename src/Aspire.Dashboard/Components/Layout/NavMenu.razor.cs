// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class NavMenu
{
    public async Task LaunchSettings()
    {
        DialogParameters parameters = new()
        {
            Title = $"Settings",
            PrimaryAction = "Close",
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Right,
            Width = "300px",
            Height = "auto"
        };

        _ = await dialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }

    private bool SidebarExpanded { get; set; }

    private void ToggleSidebarExpansion()
    {
        SidebarExpanded = !SidebarExpanded;
    }

    private string ToggleSidebarText
    {
        get
        {
            return SidebarExpanded ? "Show Less Information" : "Show More Information";
        }
    }

    private Icon ToggleSidebarIcon
    {
        get
        {
            return SidebarExpanded ? new Icons.Regular.Size24.ArrowLeft() : new Icons.Regular.Size24.ArrowRight();
        }
    }
}
