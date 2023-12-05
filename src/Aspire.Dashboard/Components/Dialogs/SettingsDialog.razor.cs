// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent, IAsyncDisposable
{
    private const float DarkThemeLuminance = 0.15f;
    private const float LightThemeLuminance = 0.95f;
    private const string ThemeSettingSystem = "System";
    private const string ThemeSettingDark = "Dark";
    private const string ThemeSettingLight = "Light";

    private string _currentSetting = ThemeSettingSystem;
    private static readonly string? s_version = GetVersionFromAssembly();

    private IJSObjectReference? _jsModule;

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required ThemeManager ThemeManager { get; set; }

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
        if (_jsModule is not null)
        {
            var newLuminanceValue = await GetBaseLayerLuminanceForSetting(newValue);

            await _jsModule.InvokeVoidAsync("setDefaultBaseLayerLuminance", newLuminanceValue);
            await _jsModule.InvokeVoidAsync("setThemeCookie", newValue);
            await _jsModule.InvokeVoidAsync("setThemeOnDocument", newValue);
        }

        _currentSetting = newValue;
        await ThemeManager.RaiseThemeChangedAsync(newValue);
    }

    private static string? GetVersionFromAssembly()
    {
        // The package version is stamped into the assembly's AssemblyInformationalVersionAttribute at build time, followed by a '+' and
        // the commit hash, e.g.:
        // [assembly: AssemblyInformationalVersion("8.0.0-preview.2.23604.7+e7762a46d31842884a0bc72c92e07ba700c99bf5")]

        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version is not null)
        {
            var plusIndex = version.IndexOf('+');

            if (plusIndex > 0)
            {
                return version[..plusIndex];
            }

            return version;
        }

        // Fallback to the file version, which is based on the CI build number, and then fallback to the assembly version, which is
        // product stable version, e.g. 8.0.0.0
        version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

        return version;
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
        if (_jsModule is not null)
        {
            var systemTheme = await _jsModule.InvokeAsync<string>("getSystemTheme");
            if (systemTheme == ThemeSettingDark)
            {
                return DarkThemeLuminance;
            }
        }

        return LightThemeLuminance;
    }

    public async ValueTask DisposeAsync()
    {
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
