// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent, IAsyncDisposable
{
    private string? _currentSetting;

    private IDisposable? _themeChangedSubscription;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    protected override void OnInitialized()
    {
        _currentSetting = ThemeManager.EffectiveTheme;

        // Handle value being changed in a different browser window.
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            var newValue = ThemeManager.Theme!;
            if (_currentSetting != newValue)
            {
                _currentSetting = newValue;
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task SettingChangedAsync()
    {
        if (_currentSetting != null)
        {
            // The theme isn't changed here. Instead, the MainLayout subscribes to the change event
            // and applies the new theme to the browser window.
            await ThemeManager.RaiseThemeChangedAsync(_currentSetting);
        }
    }

    public ValueTask DisposeAsync()
    {
        _themeChangedSubscription?.Dispose();
        return ValueTask.CompletedTask;
    }
}
