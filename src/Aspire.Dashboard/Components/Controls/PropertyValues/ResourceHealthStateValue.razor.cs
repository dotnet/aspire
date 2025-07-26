// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls.PropertyValues;

public partial class ResourceHealthStateValue
{
    private Icon? _icon;
    private Color _color;

    [Parameter, EditorRequired]
    public required string Value { get; set; }

    [Parameter, EditorRequired]
    public required string HighlightText { get; set; }

    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    protected override void OnParametersSet()
    {
        (_icon, _color) = ResourceIconHelpers.GetHealthStatusIcon(Resource.HealthStatus);
    }
}
