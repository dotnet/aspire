// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDialogContentComponent, IDisposable
{
    private string? _currentSetting;
    private List<CultureInfo> _languageOptions = null!;
    private CultureInfo? _selectedUiCulture;

    private IDisposable? _themeChangedSubscription;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected override void OnInitialized()
    {
        _languageOptions = DashboardWebApplication.LocalizedCultures
            .Select(CultureInfo.GetCultureInfo)
            .OrderBy(c => c.NativeName)
            .ToList();

        // User may also be using a variant of one of the supported cultures, in which case we should select the culture we support
        // this doesn't work for zh as we support two different zh cultures
        if (!_languageOptions.Contains(CultureInfo.CurrentUICulture)
                 && !StringComparers.Culture.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "zh")
                 && _languageOptions.FirstOrDefault(c => StringComparers.Culture.Equals(c.TwoLetterISOLanguageName, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)) is var matchedCulture)
        {
            _selectedUiCulture = matchedCulture;
        }
        // Otherwise, the user is using a language we do support or a fallback language
        else
        {
            _selectedUiCulture = CultureInfo.CurrentUICulture;
        }

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

    private async Task ThemeChangedAsync()
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

    private void OnLanguageChanged()
    {
        if (_selectedUiCulture is null || StringComparers.Culture.Equals(CultureInfo.CurrentUICulture.Name, _selectedUiCulture.Name))
        {
            return;
        }

        var uri = new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);

        NavigationManager.NavigateTo(
            DashboardUrls.SetLanguageUrl(_selectedUiCulture.Name, uri),
            forceLoad: true);
    }

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
    }
}
