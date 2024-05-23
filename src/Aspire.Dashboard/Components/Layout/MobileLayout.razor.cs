// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.CustomIcons;
using Aspire.Dashboard.Utils;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class MobileLayout
{
    private Task NavigateToAsync(string url)
    {
        NavigationManager.NavigateTo(url);
        return Task.CompletedTask;
    }

    private IEnumerable<NavMenuItemEntry> GetNavMenu()
    {
        if (DashboardClient.IsEnabled)
        {
            yield return new(
                LayoutLoc[nameof(Resources.Layout.NavMenuResourcesTab)],
                () => NavigateToAsync(DashboardUrls.ResourcesUrl()),
                NavMenu.ResourcesIcon()
            );

            yield return new(
                LayoutLoc[nameof(Resources.Layout.NavMenuConsoleLogsTab)],
                () => NavigateToAsync(DashboardUrls.ConsoleLogsUrl()),
                NavMenu.ConsoleLogsIcon()
            );
        }

        yield return new(
            LayoutLoc[nameof(Resources.Layout.NavMenuStructuredLogsTab)],
            () => NavigateToAsync(DashboardUrls.StructuredLogsUrl()),
            NavMenu.StructuredLogsIcon()
        );

        yield return new(
            LayoutLoc[nameof(Resources.Layout.NavMenuTracesTab)],
            () => NavigateToAsync(DashboardUrls.TracesUrl()),
            NavMenu.TracesIcon()
        );

        yield return new(
            LayoutLoc[nameof(Resources.Layout.NavMenuMetricsTab)],
            () => NavigateToAsync(DashboardUrls.MetricsUrl()),
            NavMenu.MetricsIcon()
        );

        yield return new(
            LayoutLoc[nameof(Resources.Layout.MainLayoutAspireRepoLink)],
            () => NavigateToAsync("https://aka.ms/dotnet/aspire/repo"),
            new AspireIcons.Size24.GitHub()
        );

        yield return new(
            LayoutLoc[nameof(Resources.Layout.MainLayoutAspireDashboardHelpLink)],
            LaunchHelpAsync,
            new Icons.Regular.Size24.QuestionCircle()
        );

        yield return new(
            LayoutLoc[nameof(Resources.Layout.MainLayoutLaunchSettings)],
            LaunchSettingsAsync,
            new Icons.Regular.Size24.Settings()
        );
    }
}
