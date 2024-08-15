// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components;

public class GridColumnManager(GridColumn[] columns)
{
    private ViewportInformation _currentViewport = null!;

    public void SetViewport(ViewportInformation viewportInformation)
    {
        _currentViewport = viewportInformation;
    }

    public bool IsColumnVisible(string columnName)
    {
        return GetColumnWidth(columns.First(column => column.Name == columnName)) is not null;
    }

    private string? GetColumnWidth(GridColumn column)
    {
        if (column.IsVisible is not null && !column.IsVisible())
        {
            return null;
        }

        if (_currentViewport.IsDesktop)
        {
            return column.DesktopWidth;
        }

        return column.MobileWidth;
    }

    public string GetGridTemplateColumns()
    {
        var visibleColumns = columns
            .Select(GetColumnWidth)
            .Where(s => s is not null)
            .Select(s => s!);

        return string.Join(" ", visibleColumns);
    }
}
