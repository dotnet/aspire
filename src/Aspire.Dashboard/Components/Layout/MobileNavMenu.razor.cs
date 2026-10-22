// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
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
    public NavigationAvailability Availability { get; set; } = NavigationAvailability.Default;

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

    private bool IsTelemetrySection => !DashboardClient.IsEnabled ||
        ResourcePaneState.Mode == ResourcePaneMode.Telemetry ||
        ResourcesLayout.ParsePaneMode(NavigationManager.ToBaseRelativePath(NavigationManager.Uri)) == ResourcePaneMode.Telemetry;

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
        if (DashboardClient.IsEnabled)
        {
            // The pages of a selection (overview, console logs, structured logs, traces and metrics) are icons at the
            // top of the resources view, so they highlight the Resources entry.
            yield return new MobileNavMenuEntry(
                Loc[nameof(Resources.Layout.NavMenuResourcesTab)],
                () => NavigateToAsync(DesktopNavMenu.GetSelectionPageUrl(NavigationManager, ResourcesLayout.ResourceTab.Overview)),
                DesktopNavMenu.ResourcesIcon(),
                ActiveIcon: DesktopNavMenu.ResourcesIcon(active: true),
                LinkMatchRegex: GetNonIndexPageRegex(
                    DashboardUrls.ResourceOverviewUrl(),
                    DashboardUrls.ConsoleLogsUrl(),
                    DashboardUrls.StructuredLogsUrl(),
                    DashboardUrls.TracesUrl(),
                    DashboardUrls.MetricsUrl()),
                IsResourceSelectionPage: true
            );

            yield return new MobileNavMenuEntry(
                Loc[nameof(Resources.Layout.NavMenuParametersTab)],
                () => NavigateToAsync(DashboardUrls.ParametersUrl()),
                DesktopNavMenu.ParametersIcon(),
                ActiveIcon: DesktopNavMenu.ParametersIcon(active: true),
                LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.ParametersUrl()),
                DisabledText: Availability.HasParameters ? null : Loc[nameof(Resources.Layout.NavMenuNoParameters)].Value
            );

            if (HasResourceTerminals)
            {
                yield return new MobileNavMenuEntry(
                    Loc[nameof(Resources.Layout.NavMenuTerminalsTab)],
                    () => NavigateToAsync(DashboardUrls.TerminalsUrl()),
                    DesktopNavMenu.TerminalsIcon(),
                    ActiveIcon: DesktopNavMenu.TerminalsIcon(active: true),
                    LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.TerminalsUrl())
                );
            }
        }
        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.NavMenuTelemetrySourcesTab)],
            () => NavigateToAsync(DashboardUrls.TelemetrySourcesUrl()),
            DesktopNavMenu.TelemetryIcon(),
            ActiveIcon: DesktopNavMenu.TelemetryIcon(active: true),
            LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.StructuredLogsUrl(), DashboardUrls.TracesUrl(), DashboardUrls.MetricsUrl()),
            DisabledText: Availability.HasTelemetrySources ? null : Loc[nameof(Resources.Layout.TelemetryPaneNoSources)].Value
        );

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

    private static Regex GetNonIndexPageRegex(params string[] pageRelativeBasePaths)
    {
        var alternatives = pageRelativeBasePaths
            .Select(Regex.Escape)
            .Select(path => $"{path}(\\?.*)?|{path}/.+");
        return new Regex($"^({string.Join("|", alternatives)})$", LinkMatchRegexOptions);
    }

    private const RegexOptions LinkMatchRegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
}
