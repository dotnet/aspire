// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

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
    public required IList<CommandViewModel> Commands { get; set; }

    [Parameter]
    public required EventCallback<CommandViewModel> CommandSelected { get; set; }

    [Parameter]
    public required EventCallback<string> OnViewDetails { get; set; }

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

        _menuItems.Add(new MenuButtonItem
        {
            Text = ControlLoc[nameof(Resources.ControlsStrings.ActionViewDetailsText)],
            Icon = s_viewDetailsIcon,
            OnClick = () => OnViewDetails.InvokeAsync(_menuButton?.MenuButtonId)
        });
 
        _menuItems.Add(new MenuButtonItem
        {
            Text = Loc[nameof(Resources.Resources.ResourceActionConsoleLogsText)],
            Icon = s_consoleLogsIcon,
            OnClick = () =>
            {
                NavigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: Resource.Name));
                return Task.CompletedTask;
            }
        });

        // Show telemetry menu items if there is telemetry for the resource.
        var hasTelemetryApplication = TelemetryRepository.GetApplicationByCompositeName(Resource.Name) != null;
        if (hasTelemetryApplication)
        {
            var telemetryTooltip = !hasTelemetryApplication ? Loc[nameof(Resources.Resources.ResourceActionTelemetryTooltip)] : string.Empty;
            _menuItems.Add(new MenuButtonItem { IsDivider = true });
            _menuItems.Add(new MenuButtonItem
            {
                Text = Loc[nameof(Resources.Resources.ResourceActionStructuredLogsText)],
                Icon = s_structuredLogsIcon,
                OnClick = () =>
                {
                    NavigationManager.NavigateTo(DashboardUrls.StructuredLogsUrl(resource: GetResourceName(Resource)));
                    return Task.CompletedTask;
                },
                Tooltip = telemetryTooltip
            });
            _menuItems.Add(new MenuButtonItem
            {
                Text = Loc[nameof(Resources.Resources.ResourceActionTracesText)],
                Icon = s_tracesIcon,
                OnClick = () =>
                {
                    NavigationManager.NavigateTo(DashboardUrls.TracesUrl(resource: GetResourceName(Resource)));
                    return Task.CompletedTask;
                },
                Tooltip = telemetryTooltip
            });
            _menuItems.Add(new MenuButtonItem
            {
                Text = Loc[nameof(Resources.Resources.ResourceActionMetricsText)],
                Icon = s_metricsIcon,
                OnClick = () =>
                {
                    NavigationManager.NavigateTo(DashboardUrls.MetricsUrl(resource: GetResourceName(Resource)));
                    return Task.CompletedTask;
                },
                Tooltip = telemetryTooltip
            });
        }

        // If display is desktop then we display highlighted commands next to the ... button.
        if (ViewportInformation.IsDesktop)
        {
            _highlightedCommands.AddRange(Commands.Where(c => c.IsHighlighted && c.State != CommandViewModelState.Hidden).Take(MaxHighlightedCount));
        }

        var menuCommands = Commands.Where(c => !_highlightedCommands.Contains(c) && c.State != CommandViewModelState.Hidden).ToList();
        if (menuCommands.Count > 0)
        {
            _menuItems.Add(new MenuButtonItem { IsDivider = true });

            foreach (var command in menuCommands)
            {
                var icon = (!string.IsNullOrEmpty(command.IconName) && CommandViewModel.ResolveIconName(command.IconName, command.IconVariant) is { } i) ? i : null;

                _menuItems.Add(new MenuButtonItem
                {
                    Text = command.DisplayName,
                    Tooltip = command.DisplayDescription,
                    Icon = icon,
                    OnClick = () => CommandSelected.InvokeAsync(command),
                    IsDisabled = command.State == CommandViewModelState.Disabled
                });
            }
        }
    }
}
