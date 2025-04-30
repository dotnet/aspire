// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model;

public static class ResourceMenuItems
{
    private static readonly Icon s_viewDetailsIcon = new Icons.Regular.Size16.Info();
    private static readonly Icon s_consoleLogsIcon = new Icons.Regular.Size16.SlideText();
    private static readonly Icon s_structuredLogsIcon = new Icons.Regular.Size16.SlideTextSparkle();
    private static readonly Icon s_tracesIcon = new Icons.Regular.Size16.GanttChart();
    private static readonly Icon s_metricsIcon = new Icons.Regular.Size16.ChartMultiple();
    private static readonly Icon s_linkIcon = new Icons.Regular.Size16.Link();

    public static void AddMenuItems(
        List<MenuButtonItem> menuItems,
        string? openingMenuButtonId,
        ResourceViewModel resource,
        NavigationManager navigationManager,
        TelemetryRepository telemetryRepository,
        Func<ResourceViewModel, string> getResourceName,
        IStringLocalizer<Resources.ControlsStrings> controlLoc,
        IStringLocalizer<Resources.Resources> loc,
        Func<string?, Task> onViewDetails,
        Func<CommandViewModel, Task> commandSelected,
        Func<ResourceViewModel, CommandViewModel, bool> isCommandExecuting,
        bool showConsoleLogsItem,
        bool showUrls)
    {
        menuItems.Add(new MenuButtonItem
        {
            Text = controlLoc[nameof(Resources.ControlsStrings.ActionViewDetailsText)],
            Icon = s_viewDetailsIcon,
            OnClick = () => onViewDetails(openingMenuButtonId)
        });

        if (showConsoleLogsItem)
        {
            menuItems.Add(new MenuButtonItem
            {
                Text = loc[nameof(Resources.Resources.ResourceActionConsoleLogsText)],
                Icon = s_consoleLogsIcon,
                OnClick = () =>
                {
                    navigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: getResourceName(resource)));
                    return Task.CompletedTask;
                }
            });
        }

        // Show telemetry menu items if there is telemetry for the resource.
        var telemetryApplication = telemetryRepository.GetApplicationByCompositeName(resource.Name);
        if (telemetryApplication != null)
        {
            menuItems.Add(new MenuButtonItem { IsDivider = true });

            if (!telemetryApplication.UninstrumentedPeer)
            {
                menuItems.Add(new MenuButtonItem
                {
                    Text = loc[nameof(Resources.Resources.ResourceActionStructuredLogsText)],
                    Tooltip = loc[nameof(Resources.Resources.ResourceActionStructuredLogsText)],
                    Icon = s_structuredLogsIcon,
                    OnClick = () =>
                    {
                        navigationManager.NavigateTo(DashboardUrls.StructuredLogsUrl(resource: getResourceName(resource)));
                        return Task.CompletedTask;
                    }
                });
            }

            menuItems.Add(new MenuButtonItem
            {
                Text = loc[nameof(Resources.Resources.ResourceActionTracesText)],
                Tooltip = loc[nameof(Resources.Resources.ResourceActionTracesText)],
                Icon = s_tracesIcon,
                OnClick = () =>
                {
                    navigationManager.NavigateTo(DashboardUrls.TracesUrl(resource: getResourceName(resource)));
                    return Task.CompletedTask;
                }
            });

            if (!telemetryApplication.UninstrumentedPeer)
            {
                menuItems.Add(new MenuButtonItem
                {
                    Text = loc[nameof(Resources.Resources.ResourceActionMetricsText)],
                    Tooltip = loc[nameof(Resources.Resources.ResourceActionMetricsText)],
                    Icon = s_metricsIcon,
                    OnClick = () =>
                    {
                        navigationManager.NavigateTo(DashboardUrls.MetricsUrl(resource: getResourceName(resource)));
                        return Task.CompletedTask;
                    }
                });
            }
        }

        var menuCommands = resource.Commands
            .Where(c => c.State != CommandViewModelState.Hidden)
            .OrderBy(c => !c.IsHighlighted)
            .ToList();
        if (menuCommands.Count > 0)
        {
            menuItems.Add(new MenuButtonItem { IsDivider = true });

            foreach (var command in menuCommands)
            {
                var icon = (!string.IsNullOrEmpty(command.IconName) && IconResolver.ResolveIconName(command.IconName, IconSize.Size16, command.IconVariant) is { } i) ? i : null;

                menuItems.Add(new MenuButtonItem
                {
                    Text = command.DisplayName,
                    Tooltip = command.DisplayDescription,
                    Icon = icon,
                    OnClick = () => commandSelected(command),
                    IsDisabled = command.State == CommandViewModelState.Disabled || isCommandExecuting(resource, command)
                });
            }
        }

        if (showUrls)
        {
            var urls = ResourceUrlHelpers.GetUrls(resource, includeInternalUrls: false, includeNonEndpointUrls: true)
                .Where(u => !string.IsNullOrEmpty(u.Url))
                .ToList();

            if (urls.Count > 0)
            {
                menuItems.Add(new MenuButtonItem { IsDivider = true });
            }

            foreach (var url in urls)
            {
                // Opens the URL in a new window when clicked.
                // It's important that this is done in the onclick event so the browser popup allows it.
                menuItems.Add(new MenuButtonItem
                {
                    Text = url.Text,
                    Tooltip = url.Url,
                    Icon = s_linkIcon,
                    AdditionalAttributes = new Dictionary<string, object>
                    {
                        ["data-openbutton"] = "true",
                        ["data-url"] = url.Url!,
                        ["data-target"] = "_blank"
                    }
                });
            }
        }
    }
}
