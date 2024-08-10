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

    private void HandleColumnHeaderClicked()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private string GetSortOptionText(IStringLocalizer<ControlsStrings> loc)
    {
        if (Grid.SortByAscending.HasValue && Column.ShowSortIcon)
        {
            if (Grid.SortByAscending is true)
            {
                return loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortDescendingLabel)];
            }
            else
            {
                return loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortAscendingLabel)];
            }
        }

        return loc[nameof(ControlsStrings.FluentDataGridHeaderCellSortLabel)];
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
