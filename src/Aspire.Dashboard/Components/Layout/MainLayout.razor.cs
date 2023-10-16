// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout
{
    private ElementReference _container;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            GlobalState.SetContainer(_container);
        }
    }

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
            Width = "300px",
            Height = "auto"
        };

        _ = await dialogService.ShowDialogAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }
}
