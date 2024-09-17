// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartFilters
{
    private const string NameColumn = nameof(NameColumn);
    private const string DimensionColumn = nameof(DimensionColumn);
    private const string FilterColumn = nameof(FilterColumn);

    [Parameter, EditorRequired]
    public required OtlpInstrumentData InstrumentData { get; init; }

    [Parameter, EditorRequired]
    public required InstrumentViewModel InstrumentViewModel { get; init; }

    [Parameter, EditorRequired]
    public required List<DimensionFilterViewModel> DimensionFilters { get; init; }

    [Parameter, EditorRequired]
    public required bool IsRenderedInsideModal { get; init; }

    public bool ShowCounts { get; set; }

    private GridColumnManager _manager = null!;
    private IList<GridColumn> _gridColumns = null!;

    protected override void OnInitialized()
    {
        _gridColumns = [
            new GridColumn(Name: NameColumn, DesktopWidth: "200px", MobileWidth: "2fr"),
            new GridColumn(Name: DimensionColumn, DesktopWidth: "1fr", MobileWidth: "1fr"),
            new GridColumn(Name: FilterColumn, DesktopWidth: "auto", MobileWidth: "auto")
        ];

        InstrumentViewModel.DataUpdateSubscriptions.Add(() =>
        {
            ShowCounts = InstrumentViewModel.ShowCount;
            return Task.CompletedTask;
        });
    }

    private void ShowCountChanged()
    {
        InstrumentViewModel.ShowCount = ShowCounts;
    }
}
