// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
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
            Height = "auto"
        };

        await DialogService.ShowDialogAsync<HelpDialog>(parameters).ConfigureAwait(true);
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
            Height = "auto"
        };

        await DialogService.ShowPanelAsync<SettingsDialog>(parameters).ConfigureAwait(true);
    }

    public async Task OnPageKeyDownAsync(KeyboardEventArgsWithPressedKeys args)
    {
        if (args is { Key: "?", ShiftKey: true })
        {
            await LaunchHelpAsync();
        }
        else if (args.Key.ToLower() == "s" && args.ShiftKey)
        {
            await LaunchSettingsAsync();
        }
        else if (args.CurrentlyHeldKeys.Contains("g"))
        {
            if (args.Key.ToLower() == "r")
            {
                NavigationManager.NavigateTo("/");
            }
            else if (args.Key.ToLower() == "c")
            {
                NavigationManager.NavigateTo("/ConsoleLogs");
            }
            else if (args.Key.ToLower() == "s")
            {
                NavigationManager.NavigateTo("/StructuredLogs");
            }
            else if (args.Key.ToLower() == "t")
            {
                NavigationManager.NavigateTo("/Traces");
            }
            else if (args.Key.ToLower() == "m")
            {
                NavigationManager.NavigateTo("/Metrics");
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
