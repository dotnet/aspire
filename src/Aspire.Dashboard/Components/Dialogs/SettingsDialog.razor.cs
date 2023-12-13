// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent, IAsyncDisposable
{
    private string _currentSetting = ThemeManager.ThemeSettingSystem;
    private static readonly string? s_version = typeof(SettingsDialog).Assembly.GetDisplayVersion();

    private IJSObjectReference? _jsModule;
    private IDisposable? _themeChangedSubscription;

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required ThemeManager ThemeManager { get; set; }

    protected override void OnInitialized()
    {
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/_content/Aspire.Dashboard/js/theme.js");
            _currentSetting = await _jsModule.InvokeAsync<string>("getThemeCookieValue");
            StateHasChanged();
        }
    }

    private async Task SettingChangedAsync(string newValue)
    {
        // The theme isn't changed here. Instead, the MainLayout subscribes to the change event
        // and applies the new theme to the browser window.
        _currentSetting = newValue;
        await ThemeManager.RaiseThemeChangedAsync(newValue);
    }

    public async ValueTask DisposeAsync()
    {
        _themeChangedSubscription?.Dispose();

        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }
    }
}
