// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent
{
    private const float DarkThemeLuminance = 0.08f;
    private const float LightThemeLuminance = 1.0f;
    private const string ThemeSettingSystem = "System";
    private const string ThemeSettingDark = "Dark";
    private const string ThemeSettingLight = "Light";

    private string _currentSetting = ThemeSettingSystem;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _currentSetting = await JS.InvokeAsync<string>("getThemeCookieValue");
            StateHasChanged();
        }
    }

    private async Task SettingChangedAsync(string newValue)
    {
        var newLuminanceValue = await GetBaseLayerLuminanceForSetting(newValue);

        await BaseLayerLuminance.SetValueFor(GlobalState.Container, newLuminanceValue);
        await JS.InvokeVoidAsync("setThemeCookie", newValue);

        _currentSetting = newValue;
    }

    private Task<float> GetBaseLayerLuminanceForSetting(string setting)
    {
        if (setting == ThemeSettingLight)
        {
            return Task.FromResult(LightThemeLuminance);
        }
        else if (setting == ThemeSettingDark)
        {
            return Task.FromResult(DarkThemeLuminance);
        }
        else // "System"
        {
            return GetSystemThemeLuminance();
        }
    }

    private async Task<float> GetSystemThemeLuminance()
    {
        var systemTheme = await JS.InvokeAsync<string>("getSystemTheme");
        if (systemTheme == ThemeSettingDark)
        {
            return DarkThemeLuminance;
        }
        else
        {
            return LightThemeLuminance;
        }
    }
}
