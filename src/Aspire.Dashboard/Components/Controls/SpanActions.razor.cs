// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components;

public partial class SpanActions : ComponentBase
{
    private static readonly Icon s_viewDetailsIcon = new Icons.Regular.Size16.Info();
    private static readonly Icon s_structuredLogsIcon = new Icons.Regular.Size16.SlideTextSparkle();

    private AspireMenuButton? _menuButton;

    [Inject]
    public required IStringLocalizer<Resources.ControlsStrings> ControlsLoc { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Parameter]
    public required EventCallback<string> OnViewDetails { get; set; }

    [Parameter]
    public required SpanWaterfallViewModel SpanViewModel { get; set; }

    private readonly List<MenuButtonItem> _menuItems = new();

    protected override void OnParametersSet()
    {
        _menuItems.Clear();

        _menuItems.Add(new MenuButtonItem
        {
            Text = ControlsLoc[nameof(Resources.ControlsStrings.ActionViewDetailsText)],
            Icon = s_viewDetailsIcon,
            OnClick = () => OnViewDetails.InvokeAsync(_menuButton?.MenuButtonId)
        });
        _menuItems.Add(new MenuButtonItem
        {
            Text = ControlsLoc[nameof(Resources.ControlsStrings.ActionStructuredLogsText)],
            Icon = s_structuredLogsIcon,
            OnClick = () =>
            {
                NavigationManager.NavigateTo(DashboardUrls.StructuredLogsUrl(spanId: SpanViewModel.Span.SpanId));
                return Task.CompletedTask;
            }
        });
    }
}
