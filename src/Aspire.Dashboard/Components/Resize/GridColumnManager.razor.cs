// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Resize;

public partial class GridColumnManager : ComponentBase, IDisposable
{
    private Dictionary<string, GridColumnView> _columnDesktopById = null!;
    private Dictionary<string, GridColumnView> _columnMobileById = null!;
    private float _availableFraction = 1;
    private ViewportInformation? _gridViewportInformation;

    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    [Parameter]
    public required List<GridColumn> Columns { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public ViewportInformation ViewportInformation => _gridViewportInformation ?? DimensionManager.ViewportInformation;

    protected override void OnInitialized()
    {
        DimensionManager.OnViewportSizeChanged += OnViewportSizeChanged;
    }

    protected override void OnParametersSet()
    {
        _columnDesktopById = Columns.Where(c => c.DesktopWidth is not null)
            .Select(c => new GridColumnView(c.Name, c.DesktopWidth!.Value, c.IsVisible))
            .ToDictionary(c => c.Name, StringComparers.GridColumn);
        _columnMobileById = Columns.Where(c => c.MobileWidth is not null)
            .Select(c => new GridColumnView(c.Name, c.MobileWidth!.Value, c.IsVisible))
            .ToDictionary(c => c.Name, StringComparers.GridColumn);

        if (ViewportInformation.IsDesktop)
        {
            SetWidths(_columnDesktopById);
        }
        else
        {
            SetWidths(_columnMobileById);
        }
    }

    private static void SetWidths(Dictionary<string, GridColumnView> columnById)
    {
        var visibleColumns = columnById.Values.Where(c => c.IsVisible?.Invoke() is null or true).ToList();
        var lastPercentageColumn = columnById.Values.Where(c => c.IsVisible?.Invoke() is null or true).LastOrDefault();
        var fractionTotal = visibleColumns.Where(c => c.Width is { Unit: WidthUnit.Fraction }).Sum(c => c.Width.Value);

        // We want percentages to add up to exactly 100% on the browser. This can be a problem with rounding.
        // The fix is to use the remaining percentage value for the value percentage column.
        var remainingPercentage = 100m;
        for (var i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];

            if (column.Width.Unit == WidthUnit.Pixels)
            {
                column.ResolvedBrowserWidth = $"{column.Width.Value}px";
            }
            else
            {
                var isLast = column == lastPercentageColumn;
                if (isLast)
                {
                    column.ResolvedBrowserWidth = $"{remainingPercentage}%";
                }
                else
                {
                    var percentage = Math.Round(column.Width.Value / fractionTotal * 100, 1);
                    column.ResolvedBrowserWidth = $"{percentage}%";

                    remainingPercentage -= percentage;
                }
            }
        }
    }

    private void OnViewportSizeChanged(object sender, ViewportSizeChangedEventArgs e)
    {
        SetViewportInformation(e.ViewportSize);
    }

    private void SetViewportInformation(ViewportSize viewportSize)
    {
        if (_availableFraction == 1)
        {
            _gridViewportInformation = null;
        }
        else
        {
            var calculatedViewportSize = new ViewportSize(
                Convert.ToInt32(viewportSize.Width * _availableFraction),
                viewportSize.Height);
            var newViewportInformation = ViewportInformation.GetViewportInformation(calculatedViewportSize);

            if (_gridViewportInformation != newViewportInformation)
            {
                _gridViewportInformation = newViewportInformation;
                _ = InvokeAsync(StateHasChanged);
            }
        }
    }

    public void SetWidthFraction(float fraction)
    {
        _availableFraction = fraction;
        SetViewportInformation(DimensionManager.ViewportSize);
    }

    /// <summary>
    /// Gets whether the column is known, visible, and has a width for the current viewport.
    /// </summary>
    public bool IsColumnVisible(string columnName)
    {
        if (GetColumnView(columnName) is not { } column)
        {
            return false;
        }

        return column.IsVisible?.Invoke() is null or true;
    }

    public string? GetColumnWidth(string columnName)
    {
        if (GetColumnView(columnName) is not { } column)
        {
            return null;
        }

        return column.ResolvedBrowserWidth;
    }

    private GridColumnView? GetColumnView(string columnName)
    {
        var columnsById = ViewportInformation.IsDesktop
            ? _columnDesktopById
            : _columnMobileById;

        if (!columnsById.TryGetValue(columnName, out var column)) // Is a known column.
        {
            return null;
        }

        return column;
    }

    public void Dispose()
    {
        DimensionManager.OnViewportSizeChanged -= OnViewportSizeChanged;
    }
}

