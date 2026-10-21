// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.CustomIcons;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Layout;

public partial class MobileNavMenu : ComponentBase, IAsyncDisposable
{
    internal const string MobileNavMenuId = "dashboard-mobile-nav-menu";

    private IJSObjectReference? _keyboardNavigation;
    private DotNetObjectReference<MobileNavMenu>? _mobileNavMenuReference;
    private bool _keyboardNavigationInitializing;
    private bool _disposed;

    [Parameter, EditorRequired]
    public required bool IsNavMenuOpen { get; set; }

    [Parameter]
    public bool HasResourceTerminals { get; set; }

    [Parameter]
    public bool HasParameters { get; set; }

    [Parameter, EditorRequired]
    public required Action CloseNavMenu { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> LaunchHelpAsync { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> LaunchAIAgentsAsync { get; set; }

    [Parameter, EditorRequired]
    public required bool IsAgentHelpEnabled { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> LaunchNotificationsAsync { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> LaunchSettingsAsync { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required ResourcePaneState ResourcePaneState { get; init; }

    private DesktopNavMenu.NavSection ActiveSection =>
        DesktopNavMenu.GetSection(NavigationManager.ToBaseRelativePath(NavigationManager.Uri), DashboardClient.IsEnabled);

    protected override void OnInitialized()
    {
        ResourcePaneState.Changed += OnResourcePaneChanged;
    }

    private void OnResourcePaneChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private Task NavigateToAsync(string url)
    {
        NavigationManager.NavigateTo(url);
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_disposed && IsNavMenuOpen && _keyboardNavigation is null && !_keyboardNavigationInitializing)
        {
            _keyboardNavigationInitializing = true;
            try
            {
                _mobileNavMenuReference ??= DotNetObjectReference.Create(this);
                var keyboardNavigation = await JS.InvokeAsync<IJSObjectReference>("initializeMobileNavMenuKeyboardNavigation", _mobileNavMenuReference, MobileNavMenuId);
                if (_disposed || !IsNavMenuOpen)
                {
                    await DisposeKeyboardNavigationAsync(keyboardNavigation);
                }
                else
                {
                    _keyboardNavigation = keyboardNavigation;
                }
            }
            finally
            {
                _keyboardNavigationInitializing = false;
            }
        }
        else if (!IsNavMenuOpen && _keyboardNavigation is not null)
        {
            await DisposeKeyboardNavigationAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        ResourcePaneState.Changed -= OnResourcePaneChanged;
        await DisposeKeyboardNavigationAsync();
        _mobileNavMenuReference?.Dispose();
    }

    [JSInvokable]
    public async Task CloseMobileNavMenuFromKeyboardAsync()
    {
        CloseNavMenu();
        await JS.InvokeVoidAsync("focusElement", MainLayout.NavigationButtonId);
    }

    [JSInvokable]
    public Task CloseMobileNavMenuFromFocusLossAsync()
    {
        CloseNavMenu();
        return Task.CompletedTask;
    }

    private async ValueTask DisposeKeyboardNavigationAsync()
    {
        if (_keyboardNavigation is { } keyboardNavigation)
        {
            _keyboardNavigation = null;
            await DisposeKeyboardNavigationAsync(keyboardNavigation);
        }
    }

    private async ValueTask DisposeKeyboardNavigationAsync(IJSObjectReference keyboardNavigation)
    {
        try
        {
            await JS.InvokeVoidAsync("disposeMobileNavMenuKeyboardNavigation", keyboardNavigation);
        }
        catch (JSDisconnectedException)
        {
            // The Blazor circuit can disconnect while the layout is being disposed.
            // In that case the browser listener is already gone with the page.
        }

        await JSInteropHelpers.SafeDisposeAsync(keyboardNavigation);
    }

    private IEnumerable<MobileNavMenuEntry> GetMobileNavMenuEntries()
    {
        foreach (var item in DesktopNavMenu.GetItems(DashboardClient.IsEnabled, HasResourceTerminals, HasParameters, Loc,
            NavigationManager.ToBaseRelativePath(NavigationManager.Uri), ResourcePaneState))
        {
            yield return new MobileNavMenuEntry(
                item.Text,
                () => NavigateToAsync(item.Href),
                item.Icon,
                ActiveIcon: item.ActiveIcon,
                Section: item.Section
            );
        }

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutAspireRepoLink)],
            async () =>
            {
                await JS.InvokeVoidAsync("open", ["https://aka.ms/aspire/repo", "_blank"]);
            },
            new AspireIcons.Size24.GitHub()
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutKeyboardShortcuts)],
            LaunchHelpAsync,
            new Icons.Regular.Size24.Keyboard()
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutAspireDashboardHelpLink)],
            async () =>
            {
                await JS.InvokeVoidAsync("open", [MainLayout.DashboardDocumentationUrl, "_blank", "noopener,noreferrer"]);
            },
            new Icons.Regular.Size24.QuestionCircle()
        );

        if (IsAgentHelpEnabled)
        {
            yield return new MobileNavMenuEntry(
                Loc[nameof(Resources.Layout.MainLayoutLaunchAIAgents)],
                LaunchAIAgentsAsync,
                new Icons.Regular.Size24.BotSparkle()
            );
        }

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutLaunchNotifications)],
            LaunchNotificationsAsync,
            new Icons.Regular.Size24.Alert()
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutLaunchSettings)],
            LaunchSettingsAsync,
            new Icons.Regular.Size24.Settings()
        );
    }

}
