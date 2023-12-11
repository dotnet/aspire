// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartDimension : ComponentBase
{
    private string? _sanitizedHtmlId;

    [Parameter, EditorRequired]
    public required DimensionFilterViewModel Model { get; init; }

    public string SanitizedHtmlId => _sanitizedHtmlId ??= StringExtensions.SanitizeHtmlId(Model.Name);

    protected void OnTagSelectionChanged(DimensionValueViewModel dimensionValue, bool isChecked)
    {
        if (isChecked)
        {
            Model.SelectedValues.Add(dimensionValue);
        }
        else
        {
            Model.SelectedValues.Remove(dimensionValue);
        }
    }
}
