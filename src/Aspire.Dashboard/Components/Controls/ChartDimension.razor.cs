// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartDimension : ComponentBase
{
    [Parameter, EditorRequired]
    public required DimensionFilterViewModel Model { get; set; }

    public string SanitizedHtmlId => StringExtensions.SanitizeHtmlId(Model.Name);

    protected void OnTagSelectionChanged(DimensionValueViewModel dimensionValue, bool isVisible)
    {
        if (isVisible)
        {
            Model.SelectedValues.Add(dimensionValue);
        }
        else
        {
            Model.SelectedValues.Remove(dimensionValue);
        }
    }
}
