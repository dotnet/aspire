// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Services;

public sealed class EnvironmentVariablesDialogService(IDialogService dialogService)
{
    public async Task ShowDialogAsync(string source, EnvironmentVariablesDialogViewModel viewModel)
    {
        DialogParameters parameters = new()
        {
            Title = $"Environment Variables for '{source}'",
            PrimaryAction = "Close",
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Width = "auto",
            Height = "auto"
        };

        _ = await dialogService.ShowDialogAsync<EnvironmentVariables>(viewModel, parameters).ConfigureAwait(true);
    }
}
