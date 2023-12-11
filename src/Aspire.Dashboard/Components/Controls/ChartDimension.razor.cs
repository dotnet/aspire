// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartDimension : ComponentBase
{
    [Parameter, EditorRequired]
    public required DimensionFilterViewModel Model { get; set; }

    protected void OnResourceTypeVisibilityChanged(DimensionValueViewModel resourceType, bool isVisible)
    {
        if (isVisible)
        {
            Model.SelectedValues.Add(resourceType);
        }
        else
        {
            Model.SelectedValues.Remove(resourceType);
        }
    }
}
