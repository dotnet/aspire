// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

public partial class PlotlyChart : ChartBase
{
    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override async Task OnChartUpdated(List<ChartTrace> traces, List<DateTimeOffset> xValues, bool tickUpdate, DateTimeOffset inProgressDataTime)
    {
        var traceDtos = traces.Select(y => new PlotlyTrace
        {
            Name = y.Name,
            Values = y.DiffValues,
            Tooltips = y.Tooltips
        }).ToArray();

        if (!tickUpdate)
        {
            // The chart mostly shows numbers but some localization is needed for displaying time ticks.
            var is24Hour = DateTimeFormatInfo.CurrentInfo.LongTimePattern.StartsWith("H", StringComparison.Ordinal);
            // Plotly uses d3-time-format https://d3js.org/d3-time-format
            var time = is24Hour ? "%H:%M:%S" : "%-I:%M:%S %p";
            var userLocale = new PlotlyUserLocale
            {
                Periods = [DateTimeFormatInfo.CurrentInfo.AMDesignator, DateTimeFormatInfo.CurrentInfo.PMDesignator],
                Time = time
            };

            await JS.InvokeVoidAsync("initializeChart",
                "plotly-chart-container",
                traceDtos,
                xValues,
                TimeProvider.ToLocal(inProgressDataTime),
                TimeProvider.ToLocal(inProgressDataTime - Duration).ToLocalTime(),
                userLocale).ConfigureAwait(false);
        }
        else
        {
            await JS.InvokeVoidAsync("updateChart",
                "plotly-chart-container",
                traceDtos,
                xValues,
                TimeProvider.ToLocal(inProgressDataTime),
                TimeProvider.ToLocal(inProgressDataTime - Duration)).ConfigureAwait(false);
        }
    }
}
