// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class SettingsDialog : IDisposable
{
    private string? _currentSetting;
    private List<CultureInfo> _languageOptions = null!;
    private CultureInfo? _selectedUiCulture;
    private TimeFormat _timeFormat;
    private IReadOnlyList<IconToggleOption<ResourceListDensity>> _densityOptions = [];
    private IReadOnlyList<IconToggleOption<GroupOverview>> _groupOverviewOptions = [];
    private IReadOnlyList<IconToggleOption<RecordingsStyle>> _recordingsStyleOptions = [];

    private IDisposable? _themeChangedSubscription;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init; }

    [Inject]
    public required DashboardDialogService DialogService { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required ILocalStorage LocalStorage { get; init; }

    [Inject]
    public required DashboardViewPreferences ViewPreferences { get; init; }

    [Inject]
    public required IDashboardRunStore RunStore { get; init; }

    [Inject]
    public required IStringLocalizer<Dashboard.Resources.Resources> ResourcesLoc { get; init; }

    protected override void OnInitialized()
    {
        _languageOptions = GlobalizationHelpers.OrderedLocalizedCultures;

        _selectedUiCulture = GlobalizationHelpers.TryGetKnownParentCulture(CultureInfo.CurrentUICulture, out var matchedCulture)
            ? matchedCulture :
            // Otherwise, Blazor has fallen back to a supported language
            CultureInfo.CurrentUICulture;

        _timeFormat = TimeProvider.ConfiguredTimeFormat;
        _densityOptions =
        [
            new(ResourceListDensity.Spacious, new Icons.Regular.Size20.TextBulletListLtr(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogResourceListDensitySpacious)]),
            new(ResourceListDensity.Comfortable, new Icons.Regular.Size20.AppsListDetail(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogResourceListDensityComfortable)]),
            new(ResourceListDensity.Compact, new Icons.Regular.Size20.AppsList(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogResourceListDensityCompact)]),
        ];
        _groupOverviewOptions =
        [
            new(GroupOverview.Summary, new Icons.Regular.Size20.Home(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogGroupOverviewSummary)]),
            new(GroupOverview.Graph, new Icons.Regular.Size20.ShareAndroid(), ResourcesLoc[nameof(Dashboard.Resources.Resources.GraphPageTitle)]),
            new(GroupOverview.Cards, new Icons.Regular.Size20.Grid(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogGroupOverviewCards)]),
            new(GroupOverview.Resources, new Icons.Regular.Size20.AppsListDetail(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogGroupOverviewResources)]),
        ];
        _recordingsStyleOptions =
        [
            new(RecordingsStyle.Panel, new Icons.Regular.Size20.History(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogRecordingsStylePanel)]),
            new(RecordingsStyle.Menu, new Icons.Regular.Size20.ChevronDownUp(), Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogRecordingsStyleMenu)]),
        ];

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
        if (_selectedUiCulture is null || string.Equals(CultureInfo.CurrentUICulture.Name, _selectedUiCulture.Name, StringComparisons.CultureName))
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

    private async Task LaunchManageDataAsync()
    {
        var parameters = new DialogParameters
        {
            Title = Loc[nameof(Dashboard.Resources.Dialogs.ManageDataDialogTitle)],
            PrimaryAction = Loc[nameof(Dashboard.Resources.Dialogs.DialogCloseButtonText)],
            SecondaryAction = string.Empty,
            Width = "800px"
        };
        await DialogService.ShowDialogAsync<ManageDataDialog>(parameters);
    }

    private async Task OnTimeFormatChanged()
    {
        TimeProvider.SetConfiguredTimeFormat(_timeFormat);
        await LocalStorage.SetAsync(BrowserStorageKeys.TimeFormat, _timeFormat);

        // Reload the page to ensure all components pick up the new format
        var uri = new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);

        NavigationManager.NavigateTo(uri, forceLoad: true);
    }

    protected override async Task OnInitializedAsync()
    {
        // The preferences are usually loaded by the views that apply them, but settings can open first.
        ViewPreferences.Changed += OnViewPreferencesChanged;
        await ViewPreferences.EnsureLoadedAsync();
    }

    private void OnViewPreferencesChanged() => InvokeAsync(StateHasChanged);

    private string FormatTimeFormatOption(TimeFormat format) => format switch
    {
        TimeFormat.System => Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogTimeFormatSystem)],
        TimeFormat.TwelveHour => Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogTimeFormatTwelveHour)],
        TimeFormat.TwentyFourHour => Loc[nameof(Dashboard.Resources.Dialogs.SettingsDialogTimeFormatTwentyFourHour)],
        _ => format.ToString()
    };

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
        ViewPreferences.Changed -= OnViewPreferencesChanged;
    }
}
