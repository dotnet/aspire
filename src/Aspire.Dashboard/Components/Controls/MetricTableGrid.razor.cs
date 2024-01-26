// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class MetricTableGrid
{
    private (Icon Icon, string Title)? GetIconAndTitleForDirection(MetricTable.ValueDirectionChange? directionChange)
    {
        return directionChange switch
        {
            MetricTable.ValueDirectionChange.Up => (new Icons.Filled.Size16.ArrowCircleUp().WithColor(Color.Success), Loc[nameof(ControlsStrings.MetricTableValueIncreased)]),
            MetricTable.ValueDirectionChange.Down => (new Icons.Filled.Size16.ArrowCircleDown().WithColor(Color.Warning), Loc[nameof(ControlsStrings.MetricTableValueDecreased)]),
            MetricTable.ValueDirectionChange.Constant => (new Icons.Filled.Size16.ArrowCircleRight().WithColor(Color.Info), Loc[nameof(ControlsStrings.MetricTableValueNoChange)]),
            _ => null
        };
    }
}
