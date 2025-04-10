// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components;

public partial class ResourceActions : ComponentBase
{
    private static readonly Icon s_viewDetailsIcon = new Icons.Regular.Size16.Info();
    private static readonly Icon s_consoleLogsIcon = new Icons.Regular.Size16.SlideText();
    private static readonly Icon s_structuredLogsIcon = new Icons.Regular.Size16.SlideTextSparkle();
    private static readonly Icon s_tracesIcon = new Icons.Regular.Size16.GanttChart();
    private static readonly Icon s_metricsIcon = new Icons.Regular.Size16.ChartMultiple();

    private AspireMenuButton? _menuButton;

    [Inject]
    public required IStringLocalizer<Resources.Resources> Loc { get; set; }

    [Inject]
    public required IStringLocalizer<Resources.ControlsStrings> ControlLoc { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Parameter]
    public required Func<CommandViewModel, Task> CommandSelected { get; set; }

    [Parameter]
    public required Func<ResourceViewModel, CommandViewModel, bool> IsCommandExecuting { get; set; }

    [Parameter]
    public required Func<string?, Task> OnViewDetails { get; set; }

    [Parameter]
    public required ResourceViewModel Resource { get; set; }

    [Parameter]
    public required Func<ResourceViewModel, string> GetResourceName { get; set; }

    [Parameter]
    public required int MaxHighlightedCount { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    private readonly List<CommandViewModel> _highlightedCommands = new();
    private readonly List<MenuButtonItem> _menuItems = new();

    protected override void OnParametersSet()
    {
        _menuItems.Clear();
        _highlightedCommands.Clear();

        AddMenuItems(
            _menuItems,
            _menuButton?.MenuButtonId,
            Resource,
            NavigationManager,
            TelemetryRepository,
            GetResourceName,
            ControlLoc,
            Loc,
            OnViewDetails,
            CommandSelected,
            IsCommandExecuting,
            showConsoleLogsItem: true);

        // If display is desktop then we display highlighted commands next to the ... button.
        if (ViewportInformation.IsDesktop)
        {
            _highlightedCommands.AddRange(Resource.Commands.Where(c => c.IsHighlighted && c.State != CommandViewModelState.Hidden).Take(MaxHighlightedCount));
        }
    }

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
        bool showConsoleLogsItem)
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
                    navigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: resource.Name));
                    return Task.CompletedTask;
                }
            });
        }

        // Show telemetry menu items if there is telemetry for the resource.
        var hasTelemetryApplication = telemetryRepository.GetApplicationByCompositeName(resource.Name) != null;
        if (hasTelemetryApplication)
        {
            menuItems.Add(new MenuButtonItem { IsDivider = true });
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
    }
}
