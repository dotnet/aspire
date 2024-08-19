// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components;

public class GridColumnManager
{
    private readonly GridColumn[] _columns;
    private readonly DimensionManager _dimensionManager;

    public GridColumnManager(GridColumn[] columns, DimensionManager dimensionManager)
    {
        if (columns.DistinctBy(c => c.Name, StringComparers.GridColumn).Count() != columns.Length)
        {
            throw new InvalidOperationException("There are duplicate columns");
        }

        _columns = columns;
        _dimensionManager = dimensionManager;
    }

    public bool IsColumnVisible(string columnId)
    {
        return GetColumnWidth(_columns.First(column => column.Name == columnId)) is not null;
    }

    private string? GetColumnWidth(GridColumn column)
    {
        if (column.IsVisible is not null && !column.IsVisible())
        {
            return null;
        }

        if (_dimensionManager.ViewportInformation.IsDesktop)
        {
            return column.DesktopWidth;
        }

        return column.MobileWidth;
    }

    public string GetGridTemplateColumns()
    {
        var visibleColumns = _columns
            .Select(GetColumnWidth)
            .Where(s => s is not null)
            .Select(s => s!);

        return string.Join(" ", visibleColumns);
    }
}
