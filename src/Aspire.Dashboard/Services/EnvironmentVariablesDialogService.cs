// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Services;

internal sealed class EnvironmentVariablesDialogService(IDialogService dialogService)
{
    public async Task ShowDialogAsync(string source, List<EnvironmentVariableViewModel> variables)
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

        _ = await dialogService.ShowDialogAsync<EnvironmentVariables>(variables, parameters).ConfigureAwait(false);
    }
}
