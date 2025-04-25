// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
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
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ConsoleLogsManager ConsoleLogsManager { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init; }

    protected override void OnInitialized()
    {
        // Order cultures in the dropdown with invariant culture. This prevents the order of languages changing when the culture changes.
        _languageOptions = [.. GlobalizationHelpers.LocalizedCultures.OrderBy(c => c.NativeName, StringComparer.InvariantCultureIgnoreCase)];

        _selectedUiCulture = GlobalizationHelpers.TryGetKnownParentCulture(_languageOptions, CultureInfo.CurrentUICulture, out var matchedCulture)
            ? matchedCulture :
            // Otherwise, Blazor has fallen back to a supported language
            CultureInfo.CurrentUICulture;

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
        if (_selectedUiCulture is null || StringComparers.CultureName.Equals(CultureInfo.CurrentUICulture.Name, _selectedUiCulture.Name))
        {
            return;
        }

        var uri = new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);

        // A cookie (CookieRequestCultureProvider.DefaultCookieName) must be set and the page reloaded to use the new culture set by the localization middleware.
        NavigationManager.NavigateTo(
            DashboardUrls.SetLanguageUrl(_selectedUiCulture.Name, uri),
            forceLoad: true);
    }

    private async Task ClearAllSignals()
    {
        TelemetryRepository.ClearAllSignals();

        await ConsoleLogsManager.UpdateFiltersAsync(new ConsoleLogsFilters { FilterAllLogsDate = TimeProvider.GetUtcNow().UtcDateTime });
    }

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
    }
}
