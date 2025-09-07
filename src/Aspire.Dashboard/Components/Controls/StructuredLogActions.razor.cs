// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components;

public partial class StructuredLogActions : ComponentBase
{
    private static readonly Icon s_viewDetailsIcon = new Icons.Regular.Size16.Info();
    private static readonly Icon s_messageOpenIcon = new Icons.Regular.Size16.Open();

    private AspireMenuButton? _menuButton;

    [Inject]
    public required IStringLocalizer<Resources.StructuredLogs> Loc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.ControlsStrings> ControlsLoc { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Dialogs> DialogsLoc { get; init; }

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Parameter]
    public required EventCallback<string> OnViewDetails { get; set; }

    [Parameter]
    public required OtlpLogEntry LogEntry { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

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
            Text = Loc[nameof(Resources.StructuredLogs.ActionLogMessageText)],
            Icon = s_messageOpenIcon,
            OnClick = async () =>
            {
                var header = Loc[nameof(Resources.StructuredLogs.StructuredLogsMessageColumnHeader)];
                await TextVisualizerDialog.OpenDialogAsync(ViewportInformation, DialogService, DialogsLoc, header, LogEntry.Message, containsSecret: false);
            }
        });
    }
}
