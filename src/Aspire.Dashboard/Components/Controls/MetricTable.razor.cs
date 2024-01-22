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
    private readonly SortedList<Metric, Metric> _metrics = new(new MetricTimeComparer());
    private static readonly List<int> s_shownPercentiles = [50, 90, 99];

    private bool _showLatestMetrics = true;
    private bool _onlyShowValueChanges;

    private IEnumerable<Metric> FilteredMetrics => _showLatestMetrics ? _metrics.Values.TakeLast(10) : _metrics.Values;
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
            foreach (var dimension in InstrumentViewModel.MatchedDimensions)
            {
                if (!dimension.Name.Equals(DimensionScope.NoDimensions))
                {
                    _anyDimensionsShown = true;
                }

                for (var i = 0; i < dimension.Values.Count; i++)
                {
                    var metricValue = dimension.Values[i];

                    ValueDirectionChange? countDirectionChange = ValueDirectionChange.Constant;
                    if (i > 0)
                    {
                        var previousValue = dimension.Values[i - 1];

                        if (metricValue.Count > previousValue.Count)
                        {
                            countDirectionChange = ValueDirectionChange.Up;
                        }
                        else if (metricValue.Count < previousValue.Count)
                        {
                            countDirectionChange = ValueDirectionChange.Down;
                        }
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
                                CountDirectionChange = countDirectionChange,
                                Percentiles = percentiles
                            });

                        static ValueDirectionChange GetDirectionChange(double? current, double? previous)
                        {
                            if (current > previous)
                            {
                                return ValueDirectionChange.Up;
                            }

                            return current < previous ? ValueDirectionChange.Down : ValueDirectionChange.Constant;
                        }
                    }
                    else
                    {
                        metrics.Add(
                            new Metric
                            {
                                DimensionName = dimension.Name, DimensionAttributes = dimension.Attributes, Value = metricValue, CountDirectionChange = countDirectionChange
                            });
                    }
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
                    _metrics.TryAdd(metrics[i], metrics[i]);
                }
                else if (!_metrics.GetValueAtIndex(i).Equals(metrics[i]))
                {
                    _metrics.SetValueAtIndex(i, metrics[i]);
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

            if (!newFilteredMetrics[oldFilteredMetrics.Count - 1].Equals(oldFilteredMetrics.Last()))
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
                var current = metrics[0].Value.Count;
                for (var i = 1; i < metrics.Count; i++)
                {
                    var count = metrics[i].Value.Count;
                    if (current == count)
                    {
                        metrics.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        current = count;
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
        public required ValueDirectionChange? CountDirectionChange { get; init; }

        public override bool Equals(object? obj)
        {
            return obj is Metric other
                && DimensionName == other.DimensionName
                && DimensionAttributes.Equals(other.DimensionAttributes)
                && Value.Equals(other.Value)
                && CountDirectionChange == other.CountDirectionChange;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DimensionName, DimensionAttributes, Value, CountDirectionChange);
        }
    }

    public class HistogramMetric : Metric
    {
        public required SortedDictionary<int, (double? Value, ValueDirectionChange Direction)> Percentiles { get; init; }
    }

    public class MetricTimeComparer : IComparer<Metric>
    {
        public int Compare(Metric? x, Metric? y)
        {
            var result = x!.Value.Start.CompareTo(y!.Value.Start);
            return result is not 0 ? result : x.Value.End.CompareTo(y.Value.End);
        }
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
}
