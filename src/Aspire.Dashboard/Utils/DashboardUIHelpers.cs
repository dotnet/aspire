// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

internal static class DashboardUIHelpers
{
    // The initial data fetch for a FluentDataGrid doesn't include a count of items to return.
    // The data grid doesn't specify a count because it doesn't know how many items fit in the UI.
    // Once it knows the height of items and the height of the grid then it specifies the desired item count
    // and then virtualization will fetch more data as needed. The problem with this is the initial fetch
    // could fetch all available data when it doesn't need to.
    //
    // If there is no count then default to a limit to avoid getting all data.
    // Given the size of rows on dashboard grids, 100 rows should always fill the grid on the screen.
    public const int DefaultDataGridResultCount = 100;

    // Don't attempt to display more than 2 highlighted commands. Many commands will take up too much space.
    public const int MaxHighlightedCommands = 2;

    public static readonly TimeSpan ToastTimeout = TimeSpan.FromMilliseconds(5000);

    public static (ColumnResizeLabels resizeLabels, ColumnSortLabels sortLabels) CreateGridLabels(IStringLocalizer<ControlsStrings> loc)
    {
        var resizeLabels = ColumnResizeLabels.Default with
        {
            ExactLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeLabel)],
            ResizeMenu = loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeButtonText)]
        };
        var sortLabels = ColumnSortLabels.Default with
        {
            SortMenu = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortButtonText)],
            SortMenuAscendingLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortAscendingButtonText)],
            SortMenuDescendingLabel = loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortDescendingButtonText)]
        };
        return (resizeLabels, sortLabels);
    }
}
