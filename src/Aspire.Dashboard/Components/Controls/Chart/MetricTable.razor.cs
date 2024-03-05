// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class MetricTable : ChartBase
{
    private SortedList<DateTimeOffset, MetricViewBase> _metrics = [];
    private string _unitColumnHeader = string.Empty;
    private IJSObjectReference? _jsModule;

    private OtlpInstrument? _instrument;
    private bool _showCount;
    private bool _onlyShowValueChanges = true;

    private readonly CancellationTokenSource _waitTaskCancellationTokenSource = new();

    private IQueryable<MetricViewBase> _metricsView => _metrics.Values.AsEnumerable().Reverse().ToList().AsQueryable();

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override async Task OnChartUpdated(List<ChartTrace> traces, List<DateTimeOffset> xValues, bool tickUpdate, DateTimeOffset inProgressDataTime)
    {
        if (!Equals(_instrument?.Name, InstrumentViewModel.Instrument?.Name) || _showCount != InstrumentViewModel.ShowCount)
        {
            _metrics.Clear();
        }

        // Store local values from view model on data update.
        // This keeps the instrument and data consistent while the view model is updated.
        _instrument = InstrumentViewModel.Instrument;
        _showCount = InstrumentViewModel.ShowCount;

        _metrics = UpdateMetrics(out var xValuesToAnnounce, traces, xValues);

        await InvokeAsync(StateHasChanged);

        if (xValuesToAnnounce.Count == 0)
        {
            return;
        }

        if (_jsModule is not null)
        {
            await Task.Delay(500, _waitTaskCancellationTokenSource.Token);

            if (_jsModule is not null)
            {
                var metricView = _metricsView.ToList();
                List<int> indices = [];

                for (var i = 0; i < metricView.Count; i++)
                {
                    if (xValuesToAnnounce.Contains(metricView[i].DateTime))
                    {
                        indices.Add(i);
                    }
                }

                await _jsModule.InvokeVoidAsync("announceDataGridRows", "metric-table-container", indices);
            }
        }
    }

    private SortedList<DateTimeOffset, MetricViewBase> UpdateMetrics(out ISet<DateTimeOffset> addedXValues, List<ChartTrace> traces, List<DateTimeOffset> xValues)
    {
        var newMetrics = new SortedList<DateTimeOffset, MetricViewBase>();

        _unitColumnHeader = traces.First().Name;

        for (var i = 0; i < xValues.Count; i++)
        {
            var xValue = xValues[i];

            KeyValuePair<DateTimeOffset, MetricViewBase>? previousMetric = newMetrics.LastOrDefault(dt => dt.Key < xValue);

            if (IsHistogramInstrument() && !_showCount)
            {
                var iTmp = i;
                var traceValuesByPercentile = traces.ToDictionary(trace => trace.Percentile!.Value, trace => trace.Values[iTmp]);
                var valueDiffs = traceValuesByPercentile.Select(kvp =>
                {
                    var (percentile, traceValue) = kvp;
                    if (traceValue is not null
                        && previousMetric?.Value is HistogramMetricView histogramMetricView
                        && histogramMetricView.Percentiles[percentile].Value is { } previousPercentileValue)
                    {
                        return traceValue.Value - previousPercentileValue;
                    }

                    return traceValue;
                }).ToList();

                if (traceValuesByPercentile.Values.All(value => value is null))
                {
                    continue;
                }

                if (_onlyShowValueChanges && valueDiffs.All(diff => DoubleEquals(diff, 0)))
                {
                    continue;
                }

                newMetrics.Add(xValue, CreateHistogramMetricView());

                MetricViewBase CreateHistogramMetricView()
                {
                    var percentiles = new SortedDictionary<int, (string Name, double? Value, ValueDirectionChange Direction)>();
                    for (var traceIndex = 0; traceIndex < traces.Count; traceIndex++)
                    {
                        var trace = traces[traceIndex];
                        percentiles.Add(trace.Percentile!.Value, (trace.Name, trace.Values[i], GetDirectionChange(valueDiffs[traceIndex])));
                    }

                    return new HistogramMetricView
                    {
                        DateTime = xValue,
                        Percentiles = percentiles
                    };
                }
            }
            else
            {
                var trace = traces.Single();
                var yValue = trace.Values[i];
                var valueDiff = yValue is not null && (previousMetric?.Value as MetricValueView)?.Value is { } previousValue ? yValue - previousValue : yValue;

                if (yValue is null)
                {
                    continue;
                }

                if (_onlyShowValueChanges && DoubleEquals(valueDiff, 0d))
                {
                    continue;
                }

                newMetrics.Add(xValue, CreateMetricView());

                MetricViewBase CreateMetricView()
                {
                    return new MetricValueView
                    {
                        DateTime = xValue,
                        Value = yValue,
                        ValueChange = GetDirectionChange(valueDiff)
                    };
                }
            }
        }

        DateTimeOffset? latestCurrentMetric = _metrics.Keys.LastOrDefault();
        addedXValues = newMetrics.Keys.Where(newKey => newKey > latestCurrentMetric).ToHashSet();
        return newMetrics;
    }

    private static bool DoubleEquals(double? a, double? b)
    {
        if (a is not null && b is not null)
        {
            return Math.Abs(a.Value - b.Value) < 0.00002; // arbitrarily small number
        }

        if ((a is null && b is not null) || (a is not null && b is null))
        {
            return false;
        }

        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Controls/Chart/MetricTable.razor.js");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private bool IsHistogramInstrument()
    {
        return _instrument?.Type == OtlpInstrumentType.Histogram;
    }

    private bool ShowPercentiles()
    {
        return IsHistogramInstrument() && !_showCount;
    }

    private Task SettingsChangedAsync() => InvokeAsync(StateHasChanged);

    private static ValueDirectionChange GetDirectionChange(double? comparisonResult)
    {
        if (comparisonResult > 0)
        {
            return ValueDirectionChange.Up;
        }

        return comparisonResult < 0 ? ValueDirectionChange.Down : ValueDirectionChange.Constant;
    }

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

    public abstract record MetricViewBase
    {
        public required DateTimeOffset DateTime { get; set; }
    }

    public record MetricValueView : MetricViewBase
    {
        public required double? Value { get; set; }
        public required ValueDirectionChange? ValueChange { get; init; }
    }

    public record HistogramMetricView : MetricViewBase
    {
        public required SortedDictionary<int, (string Name, double? Value, ValueDirectionChange Direction)> Percentiles { get; init; }
    }

    public enum ValueDirectionChange
    {
        Up,
        Down,
        Constant
    }

    private (Icon Icon, string Title)? GetIconAndTitleForDirection(ValueDirectionChange? directionChange)
    {
        return directionChange switch
        {
            ValueDirectionChange.Up => (new Icons.Filled.Size16.ArrowCircleUp().WithColor(Color.Success), Loc[nameof(ControlsStrings.MetricTableValueIncreased)]),
            ValueDirectionChange.Down => (new Icons.Filled.Size16.ArrowCircleDown().WithColor(Color.Warning), Loc[nameof(ControlsStrings.MetricTableValueDecreased)]),
            ValueDirectionChange.Constant => (new Icons.Filled.Size16.ArrowCircleRight().WithColor(Color.Info), Loc[nameof(ControlsStrings.MetricTableValueNoChange)]),
            _ => null
        };
    }

    private static string FormatMetricValue(double? value)
    {
        return value is null ? string.Empty : value.Value.ToString("F3", CultureInfo.CurrentCulture);
    }
}
