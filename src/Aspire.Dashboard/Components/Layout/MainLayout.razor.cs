// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout : IGlobalKeydownListener, IAsyncDisposable
{
    private IDisposable? _themeChangedSubscription;
    private IDisposable? _locationChangingRegistration;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _keyboardHandlers;
    private DotNetObjectReference<ShortcutManager>? _shortcutManagerReference;
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

    [Inject]
    public required ShortcutManager ShortcutManager { get; init; }

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
            _shortcutManagerReference = DotNetObjectReference.Create(ShortcutManager);
            _keyboardHandlers = await JS.InvokeAsync<IJSObjectReference>("window.registerGlobalKeydownListener", _shortcutManagerReference);
            ShortcutManager.AddGlobalKeydownListener(this);
    
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

        _openPageDialog = await DialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }

    public async Task OnPageKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.OnlyShiftPressed())
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
        else if (args.NoModifiersPressed())
        {
            var url = args.Key.ToLower() switch
            {
                "r" => DashboardUrls.ResourcesUrl(),
                "c" => DashboardUrls.ConsoleLogsUrl(),
                "s" => DashboardUrls.StructuredLogsUrl(),
                "t" => DashboardUrls.TracesUrl(),
                "m" => DashboardUrls.MetricsUrl(),
                _ => null
            };

            if (url is not null)
            {
                NavigationManager.NavigateTo(url);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shortcutManagerReference?.Dispose();
        _themeChangedSubscription?.Dispose();
        _locationChangingRegistration?.Dispose();
        ShortcutManager.RemoveGlobalKeydownListener(this);

        try
        {
            await JS.InvokeVoidAsync("window.unregisterGlobalKeydownListener", _keyboardHandlers);
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }
    }
}
