// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout
{
    private IDisposable? _themeChangedSubscription;
    private IDisposable? _locationChangingRegistration;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _keyboardHandlers;
    private IDialogReference? _openPageDialog;

    private const string SettingsDialogId = "SettingsDialog";
    private const string HelpDialogId = "HelpDialog";

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    protected override void OnInitialized()
    {
        // Theme change can be triggered from the settings dialog. This logic applies the new theme to the browser window.
        // Note that this event could be raised from a settings dialog opened in a different browser window.
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            if (_jsModule is not null)
            {
                var newValue = ThemeManager.Theme!;

                await _jsModule.InvokeVoidAsync("updateTheme", newValue);
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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/js/theme.js");
            _keyboardHandlers = await JS.InvokeAsync<IJSObjectReference>("window.registerGlobalKeydownListener", typeof(App).Assembly.GetName().Name);
            ShortcutManager.AddGlobalKeydownListener(this);

            DialogService.OnDialogCloseRequested += (reference, _) =>
            {
                if (reference.Id is HelpDialogId or SettingsDialogId)
                {
                    _openPageDialog = null;
                }
            };
        }
    }

    private async Task LaunchHelpAsync()
    {
        DialogParameters parameters = new()
        {
            Title = Loc[nameof(Resources.Layout.MainLayoutAspireDashboardHelpLink)],
            PrimaryAction = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogClose)],
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Center,
            Width = "700px",
            Height = "auto",
            Id = HelpDialogId,
            OnDialogResult = EventCallback.Factory.Create<DialogResult>(this, HandleDialogResult)
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

    private void HandleDialogResult(DialogResult dialogResult)
    {
        _openPageDialog = null;
    }

    public async Task LaunchSettingsAsync()
    {
        DialogParameters parameters = new()
        {
            Title = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogTitle)],
            PrimaryAction = Loc[nameof(Resources.Layout.MainLayoutSettingsDialogClose)],
            PrimaryActionEnabled = true,
            SecondaryAction = null,
            TrapFocus = true,
            Modal = true,
            Alignment = HorizontalAlignment.Right,
            Width = "300px",
            Height = "auto",
            Id = SettingsDialogId,
            OnDialogResult = EventCallback.Factory.Create<DialogResult>(this, HandleDialogResult)
        };

        if (_openPageDialog is not null)
        {
            if (Equals(_openPageDialog.Id, SettingsDialogId))
            {
                return;
            }

            await _openPageDialog.CloseAsync();
        }

        _openPageDialog = await DialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }

    public async Task OnPageKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.ShiftKey)
        {
            if (args.Key is "?")
            {
                await LaunchHelpAsync();
            }
            else if (args.Key.ToLower() is "s")
            {
                await LaunchSettingsAsync();
            }
        }
        else
        {
            var url = args.Key.ToLower() switch
            {
                "r" => "/",
                "c" => "/ConsoleLogs",
                "s" => "/StructuredLogs",
                "t" => "/Traces",
                "m" => "/Metrics",
                _ => null
            };

            if (url is not null)
            {
                NavigationManager.NavigateTo(url);
            }
        }
    }

    public void Dispose()
    {
        _themeChangedSubscription?.Dispose();
        _locationChangingRegistration?.Dispose();
        ShortcutManager.RemoveGlobalKeydownListener(this);
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("window.unregisterGlobalKeydownListener", _keyboardHandlers);
    }
}
