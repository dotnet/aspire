// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent, IDisposable
{
    private string? _currentSetting;

    private IDisposable? _themeChangedSubscription;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    protected override void OnInitialized()
    {
        _currentSetting = ThemeManager.SelectedTheme ?? ThemeManager.ThemeSettingSystem;

        // Handle value being changed in a different browser window.
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            var newValue = ThemeManager.SelectedTheme!;
            if (_currentSetting != newValue)
            {
                _currentSetting = newValue;
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task SettingChangedAsync()
    {
        // The field is being transiently set to null when the value changes. Maybe a bug in FluentUI?
        // This should never be set to null by the dashboard so we can ignore null values.
        if (_currentSetting != null)
        {
            // The theme isn't changed here. Instead, the MainLayout subscribes to the change event
            // and applies the new theme to the browser window.
            await ThemeManager.RaiseThemeChangedAsync(_currentSetting);
        }
    }

    private void ClearAllSignals()
    {
        TelemetryRepository.ClearAllSignals();
    }

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
    }
}
