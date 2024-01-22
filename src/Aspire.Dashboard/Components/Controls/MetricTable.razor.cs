// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using CollectionExtensions = Aspire.Dashboard.Extensions.CollectionExtensions;

namespace Aspire.Dashboard.Components;

public partial class MetricTable : ComponentBase
{
    private readonly List<Metric> _metrics = [];
    private static readonly List<int> s_shownPercentiles = [50, 90, 99];

    private bool _showLatestMetrics = true;
    private bool _onlyShowValueChanges;

    private IEnumerable<Metric> FilteredMetrics => _showLatestMetrics ? _metrics.TakeLast(10) : _metrics;
    private bool _anyDimensionsShown;

    private IJSObjectReference? _jsModule;

    [Inject]
    public required IJSRuntime JS { get; set; }

    protected override void OnInitialized()
    {
        InstrumentViewModel.DataUpdateSubscriptions.Add(OnInstrumentDataUpdate);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/_content/Aspire.Dashboard/Components/Controls/MetricTable.razor.js");
        }
    }

    private async Task OnInstrumentDataUpdate()
    {
        var oldFilteredMetrics = FilteredMetrics.ToList();

        _anyDimensionsShown = false;

        if (InstrumentViewModel.MatchedDimensions is not null)
        {
            var metrics = new List<Metric>();
            _anyDimensionsShown = InstrumentViewModel.MatchedDimensions.Any(dimension => !dimension.Name.Equals(DimensionScope.NoDimensions));
            var valuesWithDimensions = InstrumentViewModel.MatchedDimensions
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
                        var directionChange = metrics.LastOrDefault() is HistogramMetric last ? GetDirectionChange(percentileValue, last.Percentiles[percentile].Value) : ValueDirectionChange.Constant;
                        percentiles.Add(percentile, (percentileValue, directionChange));
                    }

                    metrics.Add(
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
                    metrics.Add(
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

            if (_onlyShowValueChanges && metrics.Count > 0)
            {
                RemoveDuplicateValues(metrics);
            }

            while (_metrics.Count > metrics.Count)
            {
                _metrics.RemoveAt(_metrics.Count - 1);
            }

            for (var i = 0; i < metrics.Count; i++)
            {
                if (i >= _metrics.Count)
                {
                    _metrics.Add(metrics[i]);
                }
                else if (!_metrics[i].Equals(metrics[i]))
                {
                    _metrics[i] = metrics[i];
                }
            }
        }

        await InvokeAsync(StateHasChanged);

        if (_jsModule is not null)
        {
            var newFilteredMetrics = FilteredMetrics.ToList();
            if (newFilteredMetrics.Count < oldFilteredMetrics.Count)
            {
                return;
            }

            var indices = new List<int>();

            if (oldFilteredMetrics.Count > 0 && !newFilteredMetrics[oldFilteredMetrics.Count - 1].Equals(oldFilteredMetrics.Last()))
            {
                indices.Add(oldFilteredMetrics.Count - 1);
            }

            for (var i = oldFilteredMetrics.Count; i < newFilteredMetrics.Count; i++)
            {
                indices.Add(i);
            }

            if (indices.Count > 0)
            {
                await _jsModule.InvokeVoidAsync("announceDataGridRows", "metric-table-container", indices);
            }
        }

        return;

        void RemoveDuplicateValues(IList<Metric> metrics)
        {
            if (!ShouldShowHistogram())
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
                var currentPercentiles = CalculatePercentiles((HistogramValue)metrics[0].Value);
                for (var i = 1; i < metrics.Count; i++)
                {
                    var histogramValue = (HistogramValue)metrics[i].Value;
                    var percentiles = CalculatePercentiles(histogramValue);
                    if (CollectionExtensions.Equivalent(currentPercentiles, percentiles))
                    {
                        metrics.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        currentPercentiles = percentiles;
                    }
                }

                static double?[] CalculatePercentiles(HistogramValue value)
                {
                    return [CalculatePercentile(50, value), CalculatePercentile(90, value), CalculatePercentile(99, value)];
                }
            }
        }
    }

    private static double? CalculatePercentile(int percentile, HistogramValue value)
    {
        return PlotlyChart.CalculatePercentile(percentile, value.Values, value.ExplicitBounds);
    }

    public class Metric
    {
        public required string DimensionName { get; init; }
        public required KeyValuePair<string, string>[] DimensionAttributes { get; init; }
        public required MetricValueBase Value { get; init; }
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

    private static Icon? GetIconForDirection(ValueDirectionChange? directionChange)
    {
        return directionChange switch
        {
            ValueDirectionChange.Up => new Icons.Regular.Size16.ArrowCircleUp().WithColor(Color.Success),
            ValueDirectionChange.Down => new Icons.Regular.Size16.ArrowCircleDown().WithColor(Color.Warning),
            ValueDirectionChange.Constant => new Icons.Regular.Size16.ArrowCircleRight().WithColor(Color.Info),
            _ => null
        };
    }

    private Task SettingsChangedAsync()
    {
        return InvokeAsync(StateHasChanged);
    }

    private bool ShouldShowHistogram()
    {
        return InstrumentViewModel.Instrument?.Type == OtlpInstrumentType.Histogram && !InstrumentViewModel.ShowCount;
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
}
