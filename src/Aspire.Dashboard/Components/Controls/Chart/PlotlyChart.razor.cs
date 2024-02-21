// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls.Chart;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

public partial class PlotlyChart : ChartBase
{
    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    protected override async Task OnChartUpdated(List<ChartTrace> traces, List<DateTime> xValues, bool tickUpdate, DateTime inProgressDataTime)
    {
        var traceDtos = traces.Select(y => new
        {
            name = y.Name,
            values = y.DiffValues,
            tooltips = y.Tooltips
        }).ToArray();

        if (!tickUpdate)
        {
            await JSRuntime.InvokeVoidAsync("initializeChart",
                "plotly-chart-container",
                traceDtos,
                xValues,
                inProgressDataTime.ToLocalTime(),
                (inProgressDataTime - base.Duration).ToLocalTime()).ConfigureAwait(false);
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("updateChart",
                "plotly-chart-container",
                traceDtos,
                xValues,
                inProgressDataTime.ToLocalTime(),
                (inProgressDataTime - Duration).ToLocalTime()).ConfigureAwait(false);
        }
    }
}
