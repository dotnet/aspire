// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout : IDisposable
{
    private IDisposable? _themeChangedSubscription;
    private IJSObjectReference? _jsModule;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; set; }

    [Inject]
    public required IResourceService ResourceService { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    protected override void OnInitialized()
    {
        // Theme change can be triggered from the settings dialog. This logic applies the new theme to the browser window.
        // Note that this event could be raised from a settings dialog opened in a different browser window.
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            if (_jsModule is not null)
            {
                var newValue = ThemeManager.Theme!;

                var newLuminanceValue = await GetBaseLayerLuminanceForSetting(newValue);

                await _jsModule.InvokeVoidAsync("setDefaultBaseLayerLuminance", newLuminanceValue);
                await _jsModule.InvokeVoidAsync("setThemeCookie", newValue);
                await _jsModule.InvokeVoidAsync("setThemeOnDocument", newValue);
            }
        });
    }

    private Task<float> GetBaseLayerLuminanceForSetting(string setting)
    {
        if (setting == ThemeManager.ThemeSettingLight)
        {
            return Task.FromResult(ThemeManager.LightThemeLuminance);
        }
        else if (setting == ThemeManager.ThemeSettingDark)
        {
            return Task.FromResult(ThemeManager.DarkThemeLuminance);
        }
        else // "System"
        {
            return GetSystemThemeLuminance();
        }
    }

    private async Task<float> GetSystemThemeLuminance()
    {
        if (_jsModule is not null)
        {
            var systemTheme = await _jsModule.InvokeAsync<string>("getSystemTheme");
            if (systemTheme == ThemeManager.ThemeSettingDark)
            {
                return ThemeManager.DarkThemeLuminance;
            }
        }

        return ThemeManager.LightThemeLuminance;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/_content/Aspire.Dashboard/js/theme.js");
        }
    }

    public async Task LaunchSettings()
    {
        DialogParameters parameters = new()
        {
            Title = Loc[Resources.Layout.MainLayoutSettingsDialogTitle],
            PrimaryAction = Resources.Layout.MainLayoutSettingsDialogClose,
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Right,
            Width = "300px",
            Height = "auto"
        };

        _ = await DialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
    }
}
