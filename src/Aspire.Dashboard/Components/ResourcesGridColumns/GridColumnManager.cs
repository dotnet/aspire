// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components;

public class GridColumnManager(IEnumerable<GridColumn> columns, DimensionManager dimensionManager)
{
    private readonly Dictionary<string, GridColumn> _columnById
        = columns.ToDictionary(c => c.Name, StringComparers.GridColumn);

    /// <summary>
    /// Gets whether the column is known, visible, and has a width for the current viewport.
    /// </summary>
    public bool IsColumnVisible(string columnName)
    {
        return _columnById.TryGetValue(columnName, out var column) // Is a known column.
            && GetColumnWidth(column) is not null                  // Has width for current viewport.
            && column.IsVisible?.Invoke() is null or true;         // Is visible.
    }

    /// <summary>
    /// Gets a string that can be used as the value for the grid-template-columns CSS property.
    /// For example, <c>1fr 2fr 1fr</c>.
    /// </summary>
    /// <returns></returns>
    public string GetGridTemplateColumns()
    {
        StringBuilder sb = new();

        foreach (var (_, column) in _columnById)
        {
            if (column.IsVisible?.Invoke() is null or true &&
                GetColumnWidth(column) is string width)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(width);
            }
        }

        return sb.ToString();
    }

    private string? GetColumnWidth(GridColumn column)
    {
        return dimensionManager.ViewportInformation.IsDesktop
            ? column.DesktopWidth
            : column.MobileWidth;
    }
}
