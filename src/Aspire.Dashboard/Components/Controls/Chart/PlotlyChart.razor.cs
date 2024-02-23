// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
            // The chart mostly shows numbers but some localization is needed for displaying time ticks.
            var is24Hour = DateTimeFormatInfo.CurrentInfo.LongTimePattern.StartsWith("H", StringComparison.Ordinal);
            // Plotly uses d3-time-format https://d3js.org/d3-time-format
            var time = is24Hour ? "%H:%M:%S" : "%-I:%M:%S %p";
            var userLocale = new
            {
                periods = new string[] { DateTimeFormatInfo.CurrentInfo.AMDesignator, DateTimeFormatInfo.CurrentInfo.PMDesignator },
                time = time
            };

            await JSRuntime.InvokeVoidAsync("initializeChart",
                "plotly-chart-container",
                traceDtos,
                xValues,
                inProgressDataTime.ToLocalTime(),
                (inProgressDataTime - Duration).ToLocalTime(),
                userLocale).ConfigureAwait(false);
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
