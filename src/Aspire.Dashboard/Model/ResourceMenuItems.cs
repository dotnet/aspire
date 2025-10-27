// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.CustomIcons;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Assistant.Prompts;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
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
    private static readonly Icon s_gitHubCopilotIcon = new AspireIcons.Size16.GitHubCopilot();
    private static readonly Icon s_toolboxIcon = new Icons.Regular.Size16.Toolbox();
    private static readonly Icon s_linkMultipleIcon = new Icons.Regular.Size16.LinkMultiple();

    public static void AddMenuItems(
        List<MenuButtonItem> menuItems,
        ResourceViewModel resource,
        NavigationManager navigationManager,
        TelemetryRepository telemetryRepository,
        IAIContextProvider aiContextProvider,
        Func<ResourceViewModel, string> getResourceName,
        IStringLocalizer<ControlsStrings> controlLoc,
        IStringLocalizer<Resources.Resources> loc,
        IStringLocalizer<Resources.AIAssistant> aiAssistantLoc,
        IStringLocalizer<Resources.AIPrompts> aiPromptsLoc,
        IStringLocalizer<Commands> commandsLoc,
        EventCallback onViewDetails,
        EventCallback<CommandViewModel> commandSelected,
        Func<ResourceViewModel, CommandViewModel, bool> isCommandExecuting,
        bool showConsoleLogsItem,
        bool showUrls,
        IconResolver iconResolver)
    {
        menuItems.Add(new MenuButtonItem
        {
            Text = controlLoc[nameof(ControlsStrings.ActionViewDetailsText)],
            Icon = s_viewDetailsIcon,
            OnClick = onViewDetails.InvokeAsync
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

        if (aiContextProvider.Enabled)
        {
            menuItems.Add(new MenuButtonItem
            {
                Text = aiAssistantLoc[nameof(AIAssistant.MenuTextAskGitHubCopilot)],
                Icon = s_gitHubCopilotIcon,
                OnClick = async () =>
                {
                    await aiContextProvider.LaunchAssistantSidebarAsync(
                        promptContext => PromptContextsBuilder.AnalyzeResource(
                            promptContext,
                            aiPromptsLoc.GetString(nameof(AIPrompts.PromptAnalyzeResource), resource.Name),
                            resource)).ConfigureAwait(false);
                }
            });
        }

        AddTelemetryMenuItems(menuItems, resource, navigationManager, telemetryRepository, getResourceName, loc);

        AddCommandMenuItems(menuItems, resource, loc, commandsLoc, commandSelected, isCommandExecuting, iconResolver);

        if (showUrls)
        {
            AddUrlMenuItems(menuItems, resource, loc);
        }
    }

    private static void AddUrlMenuItems(List<MenuButtonItem> menuItems, ResourceViewModel resource, IStringLocalizer<Resources.Resources> loc)
    {
        var urls = ResourceUrlHelpers.GetUrls(resource, includeInternalUrls: false, includeNonEndpointUrls: true)
            .Where(u => !string.IsNullOrEmpty(u.Url))
            .ToList();

        if (urls.Count == 0)
        {
            return;
        }

        menuItems.Add(new MenuButtonItem { IsDivider = true });

        if (urls.Count > 5)
        {
            var urlItems = new List<MenuButtonItem>();

            foreach (var url in urls)
            {
                urlItems.Add(CreateUrlMenuItem(url));
            }

            menuItems.Add(new MenuButtonItem
            {
                Text = loc[nameof(Resources.Resources.ResourceActionUrlsText)],
                Tooltip = "", // No tooltip for the commands menu item.
                Icon = s_linkMultipleIcon,
                NestedMenuItems = urlItems
            });
        }
        else
        {
            foreach (var url in urls)
            {
                menuItems.Add(CreateUrlMenuItem(url));
            }
        }
    }

    private static MenuButtonItem CreateUrlMenuItem(DisplayedUrl url)
    {
        // Opens the URL in a new window when clicked.
        // It's important that this is done in the onclick event so the browser popup allows it.
        return new MenuButtonItem
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
        };
    }

    private static void AddTelemetryMenuItems(List<MenuButtonItem> menuItems, ResourceViewModel resource, NavigationManager navigationManager, TelemetryRepository telemetryRepository, Func<ResourceViewModel, string> getResourceName, IStringLocalizer<Resources.Resources> loc)
    {
        // Show telemetry menu items if there is telemetry for the resource.
        var telemetryResource = telemetryRepository.GetResourceByCompositeName(resource.Name);
        if (telemetryResource != null)
        {
            menuItems.Add(new MenuButtonItem { IsDivider = true });

            if (!telemetryResource.UninstrumentedPeer)
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

            if (!telemetryResource.UninstrumentedPeer)
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
    }

    private static void AddCommandMenuItems(List<MenuButtonItem> menuItems, ResourceViewModel resource, IStringLocalizer<Resources.Resources> loc, IStringLocalizer<Commands> commandsLoc, EventCallback<CommandViewModel> commandSelected, Func<ResourceViewModel, CommandViewModel, bool> isCommandExecuting, IconResolver iconResolver)
    {
        var menuCommands = resource.Commands
                    .Where(c => c.State != CommandViewModelState.Hidden)
                    .ToList();

        if (menuCommands.Count == 0)
        {
            return;
        }

        var highlightedMenuCommands = menuCommands.Where(c => c.IsHighlighted).ToList();
        var otherMenuCommands = menuCommands.Where(c => !c.IsHighlighted).ToList();

        menuItems.Add(new MenuButtonItem { IsDivider = true });

        // Always show the highlighted commands first and not in a sub-menu.
        foreach (var highlightedCommand in highlightedMenuCommands)
        {
            menuItems.Add(CreateMenuItem(highlightedCommand));
        }

        // If there are more than 5 commands, we group them under a "Commands" menu item. This is done to avoid the menu going off the end of the screen.
        // A scenario where this could happen is viewing the menu for a resource and the resource is in the middle of the screen.
        if (highlightedMenuCommands.Count + otherMenuCommands.Count > 5)
        {
            var commands = new List<MenuButtonItem>();

            foreach (var command in otherMenuCommands)
            {
                commands.Add(CreateMenuItem(command));
            }

            menuItems.Add(new MenuButtonItem
            {
                Text = loc[nameof(Resources.Resources.ResourceActionCommandsText)],
                Tooltip = "", // No tooltip for the commands menu item.
                Icon = s_toolboxIcon,
                NestedMenuItems = commands
            });
        }
        else
        {
            foreach (var command in otherMenuCommands)
            {
                menuItems.Add(CreateMenuItem(command));
            }
        }

        MenuButtonItem CreateMenuItem(CommandViewModel command)
        {
            var icon = (!string.IsNullOrEmpty(command.IconName) && iconResolver.ResolveIconName(command.IconName, IconSize.Size16, command.IconVariant) is { } i) ? i : null;

            return new MenuButtonItem
            {
                Text = command.GetDisplayName(commandsLoc),
                Tooltip = command.GetDisplayDescription(commandsLoc),
                Icon = icon,
                OnClick = () => commandSelected.InvokeAsync(command),
                IsDisabled = command.State == CommandViewModelState.Disabled || isCommandExecuting(resource, command)
            };
        }
    }
}
