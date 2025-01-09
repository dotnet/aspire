// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class ClearSignalsButton : ComponentBase
{
    private static readonly Icon s_clearSelectedResourceIcon = new Icons.Regular.Size16.SelectAllOn();
    private static readonly Icon s_clearAllResourcesIcon = new Icons.Regular.Size16.Stack();

    [Inject]
    public required IStringLocalizer<ControlsStrings> ControlsStringsLoc { get; init; }

    [Parameter]
    public required SelectViewModel<ResourceTypeDetails> SelectedResource { get; set; }

    [Parameter]
    public required Func<ApplicationKey?, Task> HandleClearSignal { get; set; }

    private readonly List<MenuButtonItem> _clearMenuItems = new();

    protected override void OnParametersSet()
    {
        _clearMenuItems.Clear();

        _clearMenuItems.Add(new()
        {
            Icon = s_clearAllResourcesIcon,
            OnClick = () => HandleClearSignal(null),
            Text = ControlsStringsLoc[name: nameof(ControlsStrings.ClearAllResources)],
        });

        _clearMenuItems.Add(new()
        {
            Icon = s_clearSelectedResourceIcon,
            OnClick = () => HandleClearSignal(SelectedResource.Id?.GetApplicationKey()),
            IsDisabled = SelectedResource.Id == null,
            Text = SelectedResource.Id == null
                ? ControlsStringsLoc[nameof(ControlsStrings.ClearPendingSelectedResource)]
                : string.Format(CultureInfo.InvariantCulture, ControlsStringsLoc[name: nameof(ControlsStrings.ClearSelectedResource)], SelectedResource.Name),
        });
    }
}
