// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class MetricTable : ChartBase
{
    private readonly List<MetricView> _metrics = [];
    private bool _onlyShowValueChanges;
    private bool _anyDimensionsShown;
    private IJSObjectReference? _jsModule;

    private OtlpInstrument? _instrument;
    private bool _showCount;

    private readonly CancellationTokenSource _waitTaskCancellationTokenSource = new();

    private IQueryable<MetricView> _metricsView => _metrics.AsEnumerable().Reverse().AsQueryable();

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override Task OnChartUpdated(List<ChartTrace> traces, List<DateTime> xValues, bool tickUpdate, DateTime inProgressDataTime)
    {
        _anyDimensionsShown = false; // remove

        if (!Equals(_instrument?.Name, InstrumentViewModel.Instrument?.Name) || _showCount != InstrumentViewModel.ShowCount)
        {
            _metrics.Clear();
        }

        // Store local values from view model on data update.
        // This keeps the instrument and data consistent while the view model is updated.
        _instrument = InstrumentViewModel.Instrument;
        _showCount = InstrumentViewModel.ShowCount;

        UpdateMetrics();

        /*UpdateMetrics(InstrumentViewModel.MatchedDimensions, _metrics, IsHistogramInstrument(), _showCount, _onlyShowValueChanges, out var oldMetrics, out var indices, out _anyDimensionsShown);

        await InvokeAsync(StateHasChanged);

        if (_jsModule is not null && indices.Count > 0 && oldMetrics.Count > 0)
        {
            await Task.Delay(500, _waitTaskCancellationTokenSource.Token);

            if (_jsModule is not null)
            {
                await _jsModule.InvokeVoidAsync("announceDataGridRows", "metric-table-container", indices);
            }
        }*/

        return Task.CompletedTask;

        static void UpdateMetrics()
        {

        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Controls/Chart/MetricTable.razor.js");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task SettingsChangedAsync()
    {
        return InvokeAsync(StateHasChanged);
    }

    private bool IsHistogramInstrument()
    {
        return _instrument?.Type == OtlpInstrumentType.Histogram;
    }

    /*private static ValueDirectionChange GetDirectionChange(IComparable? current, IComparable? previous)
    {
        if (current is null && previous is null)
        {
            return ValueDirectionChange.Constant;
        }

        if (previous is null)
        {
            return ValueDirectionChange.Up;
        }

        if (current is null)
        {
            return ValueDirectionChange.Down;
        }

        return GetDirectionChange(current.CompareTo(previous));
    }

    private static ValueDirectionChange GetDirectionChange(int comparisonResult)
    {
        if (comparisonResult > 0)
        {
            return ValueDirectionChange.Up;
        }

        return comparisonResult < 0 ? ValueDirectionChange.Down : ValueDirectionChange.Constant;
    }*/

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule is { } module)
            {
                _jsModule = null;
                await _waitTaskCancellationTokenSource.CancelAsync();
                _waitTaskCancellationTokenSource.Dispose();
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }
    }

    public record MetricView
    {
        public required string DimensionName { get; init; }
        public required KeyValuePair<string, string>[] DimensionAttributes { get; init; }
        public required MetricValueBase Value { get; set; }
        public required ValueDirectionChange? CountChange { get; init; }
        public required ValueDirectionChange? ValueChange { get; init; }
    }

    public record HistogramMetricView : MetricView
    {
        public required SortedDictionary<int, (double? Value, ValueDirectionChange Direction)> Percentiles { get; init; }
    }

    public enum ValueDirectionChange
    {
        Up,
        Down,
        Constant
    }

    public enum TableType
    {
        Histogram,
        Instrument,
        Count
    }
}
