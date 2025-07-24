// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Components.Controls.PropertyValues;

public partial class ResourceStateValue
{
    private ResourceStateViewModel _stateViewModel = null!;

    [Parameter, EditorRequired]
    public required string Value { get; set; }

    [Parameter, EditorRequired]
    public required string HighlightText { get; set; }

    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IStringLocalizer<Columns> Loc { get; init; }

    protected override void OnParametersSet()
    {
        _stateViewModel = ResourceStateViewModel.GetStateViewModel(Resource, Loc);
    }
}
