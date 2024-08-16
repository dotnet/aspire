// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components;

public class GridColumnManager : IDisposable
{
    private readonly GridColumn[] _columns;
    private readonly DimensionManager _dimensionManager;
    private ViewportInformation _currentViewport;

    public GridColumnManager(GridColumn[] columns, ViewportInformation viewportInformation, DimensionManager dimensionManager)
    {
        _columns = columns;
        _currentViewport = viewportInformation;
        _dimensionManager = dimensionManager;
        _dimensionManager.OnBrowserDimensionsChanged += OnBrowserDimensionsChanged;
    }

    private void OnBrowserDimensionsChanged(object _, BrowserDimensionsChangedEventArgs args)
    {
        _currentViewport = args.ViewportInformation;
    }

    public bool IsColumnVisible(string columnName)
    {
        return GetColumnWidth(_columns.First(column => column.Name == columnName)) is not null;
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
        var visibleColumns = _columns
            .Select(GetColumnWidth)
            .Where(s => s is not null)
            .Select(s => s!);

        return string.Join(" ", visibleColumns);
    }

    public void Dispose()
    {
        _dimensionManager.OnBrowserDimensionsChanged -= OnBrowserDimensionsChanged;
    }
}
