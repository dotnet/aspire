// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout : IGlobalKeydownListener, IAsyncDisposable
{
    private bool _isNavMenuOpen;

    private IDisposable? _themeChangedSubscription;
    private IDisposable? _locationChangingRegistration;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _keyboardHandlers;
    private DotNetObjectReference<ShortcutManager>? _shortcutManagerReference;
    private DotNetObjectReference<MainLayout>? _layoutReference;
    private IDialogReference? _openPageDialog;

    private const string SettingsDialogId = "SettingsDialog";
    private const string HelpDialogId = "HelpDialog";

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required ComponentTelemetryContextProvider TelemetryContextProvider { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> DialogsLoc { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required ShortcutManager ShortcutManager { get; init; }

    [Inject]
    public required IMessageService MessageService { get; init; }

    [Inject]
    public required IOptionsMonitor<DashboardOptions> Options { get; init; }

    [Inject]
    public required ILocalStorage LocalStorage { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Theme change can be triggered from the settings dialog. This logic applies the new theme to the browser window.
        // Note that this event could be raised from a settings dialog opened in a different browser window.
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            if (_jsModule is not null)
            {
                var newValue = ThemeManager.SelectedTheme!;

                var effectiveTheme = await _jsModule.InvokeAsync<string>("updateTheme", newValue);
                ThemeManager.EffectiveTheme = effectiveTheme;
            }
        });

        // Redirect to the structured logs page if the dashboard has no resource service.
        if (!DashboardClient.IsEnabled)
        {
            _locationChangingRegistration = NavigationManager.RegisterLocationChangingHandler((context) =>
            {
                if (TargetLocationInterceptor.InterceptTargetLocation(NavigationManager.BaseUri, context.TargetLocation, out var newTargetLocation))
                {
                    context.PreventNavigation();
                    NavigationManager.NavigateTo(newTargetLocation);
                }

                return ValueTask.CompletedTask;
            });
        }

        var result = await JS.InvokeAsync<BrowserInfo>("window.getBrowserInfo");
        TimeProvider.SetBrowserTimeZone(result.TimeZone);
        TelemetryContextProvider.SetBrowserUserAgent(result.UserAgent);

        if (Options.CurrentValue.Otlp.AuthMode == OtlpAuthMode.Unsecured)
        {
            var dismissedResult = await LocalStorage.GetUnprotectedAsync<bool>(BrowserStorageKeys.UnsecuredTelemetryMessageDismissedKey);
            var skipMessage = dismissedResult.Success && dismissedResult.Value;

            if (!skipMessage)
            {
                // ShowMessageBarAsync must come after an await. Otherwise it will NRE.
                // I think this order allows the message bar provider to be fully initialized.
                await MessageService.ShowMessageBarAsync(options =>
                {
                    options.Title = Loc[nameof(Resources.Layout.MessageTelemetryTitle)];
                    options.Body = Loc[nameof(Resources.Layout.MessageTelemetryBody)];
                    options.Link = new()
                    {
                        Text = Loc[nameof(Resources.Layout.MessageTelemetryLink)],
                        Href = "https://aka.ms/dotnet/aspire/telemetry-unsecured",
                        Target = "_blank"
                    };
                    options.Intent = MessageIntent.Warning;
                    options.Section = DashboardUIHelpers.MessageBarSection;
                    options.AllowDismiss = true;
                    options.OnClose = async m =>
                    {
                        await LocalStorage.SetUnprotectedAsync(BrowserStorageKeys.UnsecuredTelemetryMessageDismissedKey, true);
                    };
                });
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/js/app-theme.js");
            _shortcutManagerReference = DotNetObjectReference.Create(ShortcutManager);
            _layoutReference = DotNetObjectReference.Create(this);
            _keyboardHandlers = await JS.InvokeAsync<IJSObjectReference>("window.registerGlobalKeydownListener", _shortcutManagerReference);
            ShortcutManager.AddGlobalKeydownListener(this);
        }
    }

    protected override void OnParametersSet()
    {
        if (ViewportInformation.IsDesktop && _isNavMenuOpen)
        {
            _isNavMenuOpen = false;
            CloseMobileNavMenu();
        }
    }

    private async Task LaunchHelpAsync()
    {
        DialogParameters parameters = new()
        {
            Title = Loc[nameof(Resources.Layout.MainLayoutAspireDashboardHelpLink)],
            DismissTitle = DialogsLoc[nameof(Resources.Dialogs.DialogCloseButtonText)],
            PrimaryAction = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogClose)],
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Center,
            Width = "700px",
            Height = "auto",
            Id = HelpDialogId,
            OnDialogClosing = EventCallback.Factory.Create<DialogInstance>(this, HandleDialogClose)
        };

        if (_openPageDialog is not null)
        {
            if (Equals(_openPageDialog.Id, HelpDialogId))
            {
                return;
            }

            await _openPageDialog.CloseAsync();
        }

        _openPageDialog = await DialogService.ShowDialogAsync<HelpDialog>(parameters).ConfigureAwait(true);
    }

    private void HandleDialogClose(DialogInstance dialogResult)
    {
        _openPageDialog = null;
    }

    public async Task LaunchSettingsAsync()
    {
        var parameters = new DialogParameters
        {
            Title = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogTitle)],
            DismissTitle = DialogsLoc[nameof(Resources.Dialogs.DialogCloseButtonText)],
            PrimaryAction = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogClose)].Value,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Right,
            Width = "300px",
            Height = "auto",
            Id = SettingsDialogId,
            OnDialogClosing = EventCallback.Factory.Create<DialogInstance>(this, HandleDialogClose)
        };

        if (_openPageDialog is not null)
        {
            if (Equals(_openPageDialog.Id, SettingsDialogId))
            {
                return;
            }

            await _openPageDialog.CloseAsync();
        }

        // Ensure the currently set theme is immediately available to display in settings dialog.
        await ThemeManager.EnsureInitializedAsync();

        if (ViewportInformation.IsDesktop)
        {
            _openPageDialog = await DialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
        }
        else
        {
            _openPageDialog = await DialogService.ShowDialogAsync<SettingsDialog>(parameters).ConfigureAwait(true);
        }
    }

    public IReadOnlySet<AspireKeyboardShortcut> SubscribedShortcuts { get; } = new HashSet<AspireKeyboardShortcut>
    {
        AspireKeyboardShortcut.Help,
        AspireKeyboardShortcut.Settings,
        AspireKeyboardShortcut.GoToResources,
        AspireKeyboardShortcut.GoToConsoleLogs,
        AspireKeyboardShortcut.GoToStructuredLogs,
        AspireKeyboardShortcut.GoToTraces,
        AspireKeyboardShortcut.GoToMetrics
    };

    public async Task OnPageKeyDownAsync(AspireKeyboardShortcut shortcut)
    {
        switch (shortcut)
        {
            case AspireKeyboardShortcut.Help:
                await LaunchHelpAsync();
                break;
            case AspireKeyboardShortcut.Settings:
                await LaunchSettingsAsync();
                break;
            case AspireKeyboardShortcut.GoToResources:
                NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl());
                break;
            case AspireKeyboardShortcut.GoToConsoleLogs:
                NavigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl());
                break;
            case AspireKeyboardShortcut.GoToStructuredLogs:
                NavigationManager.NavigateTo(DashboardUrls.StructuredLogsUrl());
                break;
            case AspireKeyboardShortcut.GoToTraces:
                NavigationManager.NavigateTo(DashboardUrls.TracesUrl());
                break;
            case AspireKeyboardShortcut.GoToMetrics:
                NavigationManager.NavigateTo(DashboardUrls.MetricsUrl());
                break;
        }
    }

    private void CloseMobileNavMenu()
    {
        _isNavMenuOpen = false;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _shortcutManagerReference?.Dispose();
        _layoutReference?.Dispose();
        _themeChangedSubscription?.Dispose();
        _locationChangingRegistration?.Dispose();
        ShortcutManager.RemoveGlobalKeydownListener(this);

        try
        {
            if (_keyboardHandlers is { } h)
            {
                await JS.InvokeVoidAsync("window.unregisterGlobalKeydownListener", h);
            }
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }

        await JSInteropHelpers.SafeDisposeAsync(_jsModule);
        await JSInteropHelpers.SafeDisposeAsync(_keyboardHandlers);
    }
}
