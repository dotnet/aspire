// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
    public required IList<GridColumn> Columns { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public ViewportInformation ViewportInformation => _gridViewportInformation ?? DimensionManager.ViewportInformation;

    protected override void OnInitialized()
    {
        DimensionManager.OnViewportSizeChanged += OnViewportSizeChanged;
        _columnById = Columns.ToDictionary(c => c.Name, StringComparers.GridColumn);
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

    /// <summary>
    /// Gets a string that can be used as the value for the grid-template-columns CSS property.
    /// For example, <c>1fr 2fr 1fr</c>.
    /// </summary>
    /// <returns></returns>
    public string GetGridTemplateColumns()
    {
        var sb = new StringBuilder();

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

