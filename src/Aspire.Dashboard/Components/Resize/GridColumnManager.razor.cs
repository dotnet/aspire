// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Resize;

public partial class GridColumnManager : ComponentBase, IDisposable
{
    private Dictionary<string, GridColumn> _columnById = null!;
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
        var desktopTotal = Columns.Where(c => c.DesktopWidth is { Unit: WidthUnit.Fraction }).Sum(c => c.DesktopWidth!.Value.Value);
        var mobileTotal = Columns.Where(c => c.MobileWidth is { Unit: WidthUnit.Fraction }).Sum(c => c.MobileWidth!.Value.Value);
        foreach (var item in Columns)
        {
            item.ResolvedDesktopWidth = ResolveWidth(item.DesktopWidth, desktopTotal);
            item.ResolvedMobileWidth = ResolveWidth(item.MobileWidth, mobileTotal);
        }

        _columnById = Columns.ToDictionary(c => c.Name, StringComparers.GridColumn);

        static string? ResolveWidth(Width? width, decimal fractionTotal)
        {
            if (width is not { } w)
            {
                return null;
            }

            return w.Unit switch
            {
                WidthUnit.Fraction => $"{Math.Round(w.Value / fractionTotal * 100, 1)}%",
                WidthUnit.Pixels => $"{w.Value}px",
                _ => throw new NotSupportedException($"Unsupported width unit: {w.Unit}")
            };
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
        return _columnById.TryGetValue(columnName, out var column) // Is a known column.
            && GetColumnWidth(column) is not null                  // Has width for current viewport.
            && column.IsVisible?.Invoke() is null or true;         // Is visible.
    }

    public string? GetColumnWidth(string columnName)
    {
        if (!_columnById.TryGetValue(columnName, out var column)) // Is a known column.
        {
            return null;
        }

        var viewportInformation = _gridViewportInformation ?? DimensionManager.ViewportInformation;

        return viewportInformation.IsDesktop
            ? column.ResolvedDesktopWidth
            : column.ResolvedMobileWidth;
    }

    private Width? GetColumnWidth(GridColumn column)
    {
        var viewportInformation = _gridViewportInformation ?? DimensionManager.ViewportInformation;

        return viewportInformation.IsDesktop
            ? column.DesktopWidth
            : column.MobileWidth;
    }

    public void Dispose()
    {
        DimensionManager.OnViewportSizeChanged -= OnViewportSizeChanged;
    }
}

