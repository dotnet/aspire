// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartFilters
{
    [Parameter, EditorRequired]
    public required OtlpInstrumentData Instrument { get; set; }

    [Parameter, EditorRequired]
    public required InstrumentViewModel InstrumentViewModel { get; set; }

    [Parameter, EditorRequired]
    public required List<DimensionFilterViewModel> DimensionFilters { get; set; }

    private ColumnResizeLabels _resizeLabels = ColumnResizeLabels.Default;
    private ColumnSortLabels _sortLabels = ColumnSortLabels.Default;

    public bool ShowCounts { get; set; }

    protected override void OnInitialized()
    {
        (_resizeLabels, _sortLabels) = DashboardUIHelpers.CreateGridLabels(Loc);

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
