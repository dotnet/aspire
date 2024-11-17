// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls.Grid;

public partial class AspireFluentDataGridHeaderCell<T> : ComponentBase
{
    [Parameter]
    public required ColumnBase<T> Column { get; set; }

    [Parameter]
    public required FluentDataGrid<T> Grid { get; set; }

    [Inject]
    public required IStringLocalizer<ControlsStrings> Loc { get; init; }

    private bool _isMenuOpen;
    private readonly string _columnId = $"column-header{Guid.NewGuid():N}";

    private string? Tooltip => Column.Tooltip ? Column.Title : null;

    private void HandleKeyDown(FluentKeyCodeEventArgs e)
    {
        if (e.CtrlKey && e.Key == KeyCode.Enter)
        {
            Grid.RemoveSortByColumnAsync(Column);
        }
    }

    public bool AnyColumnActionEnabled => Column.Sortable is true || Grid.ResizableColumns;

    private async Task HandleColumnHeaderClickedAsync()
    {
        if (Column.Sortable is true && Grid.ResizableColumns)
        {
            _isMenuOpen = !_isMenuOpen;
        }
        else if (Column.Sortable is true && !Grid.ResizableColumns)
        {
            await Grid.SortByColumnAsync(Column);
        }
        else if (Column.Sortable is not true && Grid.ResizableColumns)
        {
            await Grid.ShowColumnOptionsAsync(Column);
        }
    }

    private string GetSortOptionText()
    {
        if (Grid.SortByAscending.HasValue && Column.ShowSortIcon)
        {
            if (Grid.SortByAscending is true)
            {
                return Loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortDescendingButtonText)];
            }
            else
            {
                return Loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortAscendingButtonText)];
            }
        }

        return Loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortButtonText)];
    }
}

internal static class AspireFluentDataGridHeaderCell
{
    public static RenderFragment<ColumnBase<T>> RenderHeaderContent<T>(FluentDataGrid<T>? grid)
    {
        Debug.Assert(grid is not null);

        return GetHeaderContent;

        RenderFragment GetHeaderContent(ColumnBase<T> value) => builder =>
        {
            builder.OpenComponent<AspireFluentDataGridHeaderCell<T>>(0);
            builder.AddAttribute(1, nameof(AspireFluentDataGridHeaderCell<T>.Column), value);
            builder.AddAttribute(2, nameof(AspireFluentDataGridHeaderCell<T>.Grid), grid);
            builder.CloseComponent();
        };
    }

    public static string GetResizeLabel(IStringLocalizer<ControlsStrings> loc)
    {
        return loc[nameof(ControlsStrings.FluentDataGridHeaderCellResizeLabel)];
    }
}
