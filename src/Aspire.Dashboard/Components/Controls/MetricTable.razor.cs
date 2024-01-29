// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using CollectionExtensions = Aspire.Dashboard.Extensions.CollectionExtensions;

namespace Aspire.Dashboard.Components.Controls;

public partial class MetricTable : ComponentBase
{
    private static readonly List<int> s_shownPercentiles = [50, 90, 99];

    private readonly List<Metric> _metrics = [];
    private bool _onlyShowValueChanges;
    private bool _anyDimensionsShown;
    private IJSObjectReference? _jsModule;

    private OtlpInstrument? _instrument;
    private bool _showCount;

    private IQueryable<Metric> _metricsView => _metrics.AsEnumerable().Reverse().AsQueryable();

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Parameter, EditorRequired]
    public required InstrumentViewModel InstrumentViewModel { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    protected override void OnInitialized()
    {
        InstrumentViewModel.DataUpdateSubscriptions.Add(OnInstrumentDataUpdate);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Controls/MetricTable.razor.js");
        }
    }

    internal static void UpdateMetrics(
        List<DimensionScope>? matchedDimensions,
        List<Metric> currentMetrics,
        bool shouldShowHistogram,
        bool onlyShowValueChanges,
        out List<Metric> oldMetrics,
        out List<int> addedIndices,
        out bool anyDimensionsShown)
    {
        oldMetrics = [.. currentMetrics];

        anyDimensionsShown = false;

        if (matchedDimensions is not null)
        {
            var newMetrics = new List<Metric>();
            anyDimensionsShown = matchedDimensions.Any(dimension => !dimension.Name.Equals(DimensionScope.NoDimensions));
            var valuesWithDimensions = matchedDimensions
                .SelectMany(dimension => dimension.Values.Select(value => (value, dimension))).ToList();

            valuesWithDimensions.Sort((a, b) =>
            {
                var result = a.value.Start.CompareTo(b.value.Start);
                return result is not 0 ? result : a.value.End.CompareTo(b.value.End);
            });

            for (var i = 0; i < valuesWithDimensions.Count; i++)
            {
                var (metricValue, dimension) = valuesWithDimensions[i];

                ValueDirectionChange? countChange = ValueDirectionChange.Constant;
                ValueDirectionChange? valueChange = ValueDirectionChange.Constant;
                if (i > 0)
                {
                    var (previousValue, _) = valuesWithDimensions[i - 1];

                    countChange = GetDirectionChange(metricValue.Count, previousValue.Count);
                    valueChange = metricValue.TryCompare(previousValue, out var comparisonResult) ? GetDirectionChange(comparisonResult) : null;
                }

                if (metricValue is HistogramValue histogramValue)
                {
                    var percentiles = new SortedDictionary<int, (double? Value, ValueDirectionChange Direction)>();
                    foreach (var percentile in s_shownPercentiles)
                    {
                        var percentileValue = CalculatePercentile(percentile, histogramValue);
                        var directionChange = newMetrics.LastOrDefault() is HistogramMetric last ? GetDirectionChange(percentileValue, last.Percentiles[percentile].Value) : ValueDirectionChange.Constant;
                        percentiles.Add(percentile, (percentileValue, directionChange));
                    }

                    newMetrics.Add(
                        new HistogramMetric
                        {
                            DimensionName = dimension.Name,
                            DimensionAttributes = dimension.Attributes,
                            Value = metricValue,
                            CountChange = countChange,
                            ValueChange = valueChange,
                            Percentiles = percentiles
                        });

                }
                else
                {
                    newMetrics.Add(
                        new Metric
                        {
                            DimensionName = dimension.Name,
                            DimensionAttributes = dimension.Attributes,
                            Value = metricValue,
                            CountChange = countChange,
                            ValueChange = valueChange
                        });
                }
            }

            if (onlyShowValueChanges && newMetrics.Count > 0)
            {
                RemoveDuplicateValues(newMetrics);
            }

            while (currentMetrics.Count > newMetrics.Count)
            {
                currentMetrics.RemoveAt(currentMetrics.Count - 1);
            }

            for (var i = 0; i < newMetrics.Count; i++)
            {
                var newMetric = newMetrics[i];
                if (i >= currentMetrics.Count)
                {
                    currentMetrics.Add(newMetric);
                }
                else if (!currentMetrics[i].Equals(newMetrics[i]) || !currentMetrics[i].Value.End.Equals(newMetric.Value.End))
                {
                    currentMetrics[i] = newMetrics[i];
                }
            }
        }

        addedIndices = [];

        if (currentMetrics.Count < oldMetrics.Count)
        {
            return;
        }

        if (oldMetrics.Count > 0 && !currentMetrics[oldMetrics.Count - 1].Equals(oldMetrics.Last()))
        {
            addedIndices.Add(currentMetrics.Count - (oldMetrics.Count - 1) - 1);
        }

        for (var i = oldMetrics.Count; i < currentMetrics.Count; i++)
        {
            addedIndices.Add(currentMetrics.Count - i - 1);
        }

        return;

        void RemoveDuplicateValues(IList<Metric> metrics)
        {
            if (!shouldShowHistogram)
            {
                var start = metrics[0].Value;
                for (var i = 1; i < metrics.Count; i++)
                {
                    var current = metrics[i].Value;
                    if (current.TryCompare(start, out var comparisonResult) && comparisonResult == 0)
                    {
                        metrics.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        start = current;
                    }
                }
            }
            else
            {
                var startPercentiles = CalculatePercentiles((HistogramValue)metrics[0].Value);
                var startMetric = metrics[0];
                for (var i = 1; i < metrics.Count; i++)
                {
                    var currentMetric = metrics[i];

                    var histogramValue = (HistogramValue)currentMetric.Value;
                    var percentiles = CalculatePercentiles(histogramValue);
                    if (CollectionExtensions.Equivalent(startPercentiles, percentiles))
                    {
                        var metricValueWithUpdatedEnd = MetricValueBase.Clone(startMetric.Value);
                        metricValueWithUpdatedEnd.End = currentMetric.Value.End;
                        startMetric.Value = metricValueWithUpdatedEnd;

                        metrics.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        startPercentiles = percentiles;
                        startMetric = currentMetric;
                    }
                }

                static double?[] CalculatePercentiles(HistogramValue value)
                {
                    return [CalculatePercentile(50, value), CalculatePercentile(90, value), CalculatePercentile(99, value)];
                }
            }
        }
    }

    private async Task OnInstrumentDataUpdate()
    {
        if (!Equals(_instrument?.Name, InstrumentViewModel.Instrument?.Name) || _showCount != InstrumentViewModel.ShowCount)
        {
            _metrics.Clear();
        }

        // Store local values from view model on data update.
        // This keeps the instrument and data consistent while the view model is updated.
        _instrument = InstrumentViewModel.Instrument;
        _showCount = InstrumentViewModel.ShowCount;

        UpdateMetrics(InstrumentViewModel.MatchedDimensions, _metrics, ShouldShowHistogram(), _onlyShowValueChanges, out var oldMetrics, out var indices, out _anyDimensionsShown);

        await InvokeAsync(StateHasChanged);

        if (_jsModule is not null && indices.Count > 0 && oldMetrics.Count > 0)
        {
            await Task.Delay(500);
            await _jsModule.InvokeVoidAsync("announceDataGridRows", "metric-table-container", indices);
        }
    }

    internal static double? CalculatePercentile(int percentile, HistogramValue value)
    {
        return PlotlyChart.CalculatePercentile(percentile, value.Values, value.ExplicitBounds);
    }

    public class Metric
    {
        public required string DimensionName { get; init; }
        public required KeyValuePair<string, string>[] DimensionAttributes { get; init; }
        public required MetricValueBase Value { get; set; }
        public required ValueDirectionChange? CountChange { get; init; }
        public required ValueDirectionChange? ValueChange { get; init; }

        public override bool Equals(object? obj)
        {
            return obj is Metric other
                && DimensionName == other.DimensionName
                && DimensionAttributes.Equals(other.DimensionAttributes)
                && Value.Equals(other.Value)
                && CountChange == other.CountChange;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DimensionName, DimensionAttributes, Value, CountChange);
        }
    }

    public class HistogramMetric : Metric
    {
        public required SortedDictionary<int, (double? Value, ValueDirectionChange Change)> Percentiles { get; init; }
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

    private Task SettingsChangedAsync()
    {
        return InvokeAsync(StateHasChanged);
    }

    private bool ShouldShowHistogram()
    {
        return IsHistogramInstrument() && !_showCount;
    }

    private bool IsHistogramInstrument()
    {
        return _instrument?.Type == OtlpInstrumentType.Histogram;
    }

    private static ValueDirectionChange GetDirectionChange(IComparable? current, IComparable? previous)
    {
        if (current is null && previous is null)
        {
            return ValueDirectionChange.Constant;
        }
        else if (previous is null)
        {
            return ValueDirectionChange.Up;
        }
        else if (current is null)
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
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
            // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
        }
    }
}
