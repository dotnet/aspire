// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Resources;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components;

public partial class PlotlyChart : ComponentBase
{
    private const int GraphPointCount = 30;

    private TimeSpan _tickDuration;
    private DateTime _lastUpdateTime;
    private DateTime _currentDataStartTime;
    private List<KeyValuePair<string, string>[]>? _renderedDimensionAttributes;
    private OtlpInstrumentKey? _renderedInstrument;
    private string? _renderedTheme;
    private bool _renderedShowCount;

    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    [Parameter, EditorRequired]
    public required InstrumentViewModel InstrumentViewModel { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    protected override void OnInitialized()
    {
        _currentDataStartTime = GetCurrentDataTime();
        InstrumentViewModel.OnDataUpdate = OnInstrumentDataUpdate;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (InstrumentViewModel.Instrument is null || InstrumentViewModel.MatchedDimensions is null)
        {
            return;
        }

        var inProgressDataTime = GetCurrentDataTime();

        while (_currentDataStartTime.Add(_tickDuration) < inProgressDataTime)
        {
            _currentDataStartTime = _currentDataStartTime.Add(_tickDuration);
        }

        var dimensionAttributes = InstrumentViewModel.MatchedDimensions.Select(d => d.Attributes).ToList();
        if (_renderedInstrument is null || _renderedInstrument != InstrumentViewModel.Instrument.GetKey() ||
            _renderedDimensionAttributes is null || !_renderedDimensionAttributes.SequenceEqual(dimensionAttributes) ||
            _renderedTheme != InstrumentViewModel.Theme ||
            _renderedShowCount != InstrumentViewModel.ShowCount)
        {
            // Dimensions (or entire chart) has changed. Re-render the entire chart.
            _renderedInstrument = InstrumentViewModel.Instrument.GetKey();
            _renderedDimensionAttributes = dimensionAttributes;
            _renderedTheme = InstrumentViewModel.Theme;
            _renderedShowCount = InstrumentViewModel.ShowCount;
            await UpdateChart(tickUpdate: false, inProgressDataTime).ConfigureAwait(false);
        }
        else if (_lastUpdateTime.Add(TimeSpan.FromSeconds(0.2)) < DateTime.UtcNow)
        {
            // Throttle how often the chart is updated.
            _lastUpdateTime = DateTime.UtcNow;
            await UpdateChart(tickUpdate: true, inProgressDataTime).ConfigureAwait(false);
        }
    }

    protected override void OnParametersSet()
    {
        _tickDuration = Duration / GraphPointCount;
    }

    private Task OnInstrumentDataUpdate()
    {
        return InvokeAsync(StateHasChanged);
    }

    private static DateTime GetCurrentDataTime()
    {
        return DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)); // Compensate for delay in receiving metrics from sevices.;
    }

    private sealed class Trace
    {
        public required string Name { get; init; }
        public List<double?> Values { get; } = new();
        public List<double?> DiffValues { get; } = new();
        public List<string?> Tooltips { get; } = new();
    }

    private (List<Trace> Y, List<DateTime> X) CalculateHistogramValues(List<DimensionScope> dimensions, int pointCount, bool tickUpdate, DateTime inProgressDataTime, string yLabel)
    {
        var pointDuration = Duration / pointCount;
        var traces = new Dictionary<int, Trace>
        {
            [50] = new Trace { Name = $"P50 {yLabel}" },
            [90] = new Trace { Name = $"P90 {yLabel}" },
            [99] = new Trace { Name = $"P99 {yLabel}" },
        };
        var xValues = new List<DateTime>();
        var startDate = _currentDataStartTime;
        DateTime? firstPointEndTime = null;

        // Generate the points in reverse order so that the chart is drawn from right to left.
        // Add a couple of extra points to the end so that the chart is drawn all the way to the right edge.
        for (var pointIndex = 0; pointIndex < (pointCount + 2); pointIndex++)
        {
            var start = CalcOffset(pointIndex, startDate, pointDuration);
            var end = CalcOffset(pointIndex - 1, startDate, pointDuration);
            firstPointEndTime ??= end;

            xValues.Add(end.ToLocalTime());

            if (!TryCalculateHistogramPoints(dimensions, start, end, traces))
            {
                foreach (var trace in traces)
                {
                    trace.Value.Values.Add(null);
                }
            }
        }

        foreach (var item in traces)
        {
            item.Value.Values.Reverse();
        }
        xValues.Reverse();

        if (tickUpdate && TryCalculateHistogramPoints(dimensions, firstPointEndTime!.Value, inProgressDataTime, traces))
        {
            xValues.Add(inProgressDataTime.ToLocalTime());
        }

        var diffValues = new List<double>();
        var tooltips = new List<string?>();
        Trace? previousValues = null;
        foreach (var trace in traces.OrderBy(kvp => kvp.Key))
        {
            var currentTrace = trace.Value;

            for (var i = 0; i < currentTrace.Values.Count; i++)
            {
                double? diffValue = (previousValues != null)
                    ? currentTrace.Values[i] - previousValues.Values[i] ?? 0
                    : currentTrace.Values[i];

                if (diffValue > 0)
                {
                    currentTrace.Tooltips.Add(FormatTooltip(currentTrace.Name, currentTrace.Values[i].GetValueOrDefault(), xValues[i]));
                }
                else
                {
                    currentTrace.Tooltips.Add(null);
                }

                currentTrace.DiffValues.Add(diffValue);
            }

            previousValues = currentTrace;
        }
        return (traces.Select(kvp => kvp.Value).ToList(), xValues);
    }

    private string FormatTooltip(string name, double yValue, DateTime xValue)
    {
        return $"<b>{InstrumentViewModel.Instrument?.Name}</b><br />{name}: {yValue.ToString("##,0.######", CultureInfo.InvariantCulture)}<br />Time: {xValue.ToString("h:mm:ss tt", CultureInfo.InvariantCulture)}";
    }

    private static HistogramValue GetHistogramValue(MetricValueBase metric)
    {
        if (metric is HistogramValue histogramValue)
        {
            return histogramValue;
        }

        throw new InvalidOperationException("Unexpected metric type: " + metric.GetType());
    }

    private static bool TryCalculateHistogramPoints(List<DimensionScope> dimensions, DateTime start, DateTime end, Dictionary<int, Trace> traces)
    {
        var hasValue = false;

        ulong[]? currentBucketCounts = null;
        double[]? explicitBounds = null;

        start = start.Subtract(TimeSpan.FromSeconds(1));
        end = end.Add(TimeSpan.FromSeconds(1));

        foreach (var dimension in dimensions)
        {
            for (var i = dimension.Values.Count - 1; i >= 0; i--)
            {
                var metric = dimension.Values[i];
                if (metric.Start >= start && metric.Start <= end)
                {
                    var histogramValue = GetHistogramValue(metric);

                    // Only use the first recorded entry if it is the beginning of data.
                    // We can verify the first entry is the beginning of data by checking if the number of buckets equals the total count.
                    if (i == 0 && CountBuckets(histogramValue) != histogramValue.Count)
                    {
                        continue;
                    }

                    explicitBounds ??= histogramValue.ExplicitBounds;

                    var previousHistogramValues = i > 0 ? GetHistogramValue(dimension.Values[i - 1]).Values : null;

                    if (currentBucketCounts is null)
                    {
                        currentBucketCounts = new ulong[histogramValue.Values.Length];
                    }
                    else if (currentBucketCounts.Length != histogramValue.Values.Length)
                    {
                        throw new InvalidOperationException("Histogram values changed size");
                    }

                    for (var valuesIndex = 0; valuesIndex < histogramValue.Values.Length; valuesIndex++)
                    {
                        var newValue = histogramValue.Values[valuesIndex];

                        if (previousHistogramValues != null)
                        {
                            // Histogram values are culmulative, so subtract the previous value to get the diff.
                            newValue -= previousHistogramValues[valuesIndex];
                        }

                        currentBucketCounts[valuesIndex] += newValue;
                    }

                    hasValue = true;
                }
            }
        }
        if (hasValue)
        {
            foreach (var percentileValues in traces)
            {
                var percentileValue = CalculatePercentile(percentileValues.Key, currentBucketCounts!, explicitBounds!);
                percentileValues.Value.Values.Add(percentileValue);
            }
        }
        return hasValue;
    }

    private static ulong CountBuckets(HistogramValue histogramValue)
    {
        ulong value = 0ul;
        for (var i = 0; i < histogramValue.Values.Length; i++)
        {
            value += histogramValue.Values[i];
        }
        return value;
    }

    private static double? CalculatePercentile(int percentile, ulong[] counts, double[] explicitBounds)
    {
        if (percentile < 0 || percentile > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be between 0 and 100.");
        }

        var totalCount = 0ul;
        foreach (var count in counts)
        {
            totalCount += count;
        }

        var targetCount = (percentile / 100.0) * totalCount;
        var accumulatedCount = 0ul;

        for (var i = 0; i < explicitBounds.Length; i++)
        {
            accumulatedCount += counts[i];

            if (accumulatedCount >= targetCount)
            {
                return explicitBounds[i];
            }
        }

        // If the percentile is larger than any bucket value, return the last value
        return explicitBounds[explicitBounds.Length - 1];
    }

    private (List<Trace> Y, List<DateTime> X) CalculateChartValues(List<DimensionScope> dimensions, int pointCount, bool tickUpdate, DateTime inProgressDataTime, string yLabel)
    {
        var pointDuration = Duration / pointCount;
        var yValues = new List<double?>();
        var xValues = new List<DateTime>();
        var startDate = _currentDataStartTime;
        DateTime? firstPointEndTime = null;

        // Generate the points in reverse order so that the chart is drawn from right to left.
        // Add a couple of extra points to the end so that the chart is drawn all the way to the right edge.
        for (var pointIndex = 0; pointIndex < (pointCount + 2); pointIndex++)
        {
            var start = CalcOffset(pointIndex, startDate, pointDuration);
            var end = CalcOffset(pointIndex - 1, startDate, pointDuration);
            firstPointEndTime ??= end;

            xValues.Add(end.ToLocalTime());

            if (TryCalculatePoint(dimensions, start, end, out var tickPointValue))
            {
                yValues.Add(tickPointValue);
            }
            else
            {
                yValues.Add(null);
            }
        }

        yValues.Reverse();
        xValues.Reverse();

        if (tickUpdate && TryCalculatePoint(dimensions, firstPointEndTime!.Value, inProgressDataTime, out var inProgressPointValue))
        {
            yValues.Add(inProgressPointValue);
            xValues.Add(inProgressDataTime.ToLocalTime());
        }

        var trace = new Trace
        {
            Name = yLabel
        };

        for (var i = 0; i < xValues.Count; i++)
        {
            trace.Values.AddRange(yValues);
            trace.DiffValues.AddRange(yValues);
            if (yValues[i] is not null)
            {
                trace.Tooltips.Add(FormatTooltip(yLabel, yValues[i]!.Value, xValues[i]));
            }
            else
            {
                trace.Tooltips.Add(null);
            }
        }

        return ([trace], xValues);
    }

    private static bool TryCalculatePoint(List<DimensionScope> dimensions, DateTime start, DateTime end, out double pointValue)
    {
        var hasValue = false;
        pointValue = 0d;

        foreach (var dimension in dimensions)
        {
            var dimensionValue = 0d;
            for (var i = dimension.Values.Count - 1; i >= 0; i--)
            {
                var metric = dimension.Values[i];
                if ((metric.Start <= end && metric.End >= start) || (metric.Start >= start && metric.End <= end))
                {
                    var value = metric switch
                    {
                        MetricValue<long> longMetric => longMetric.Value,
                        MetricValue<double> doubleMetric => doubleMetric.Value,
                        HistogramValue histogramValue => histogramValue.Count,
                        _ => 0// throw new InvalidOperationException("Unexpected metric type: " + metric.GetType())
                    };

                    dimensionValue = Math.Max(value, dimensionValue);
                    hasValue = true;
                }
            }

            pointValue += dimensionValue;
        }

        // JS interop doesn't support serializing NaN values.
        if (double.IsNaN(pointValue))
        {
            pointValue = default;
            return false;
        }

        return hasValue;
    }

    private static DateTime CalcOffset(int pointIndex, DateTime now, TimeSpan pointDuration)
    {
        return now.Subtract(pointDuration * pointIndex);
    }

    private async Task UpdateChart(bool tickUpdate, DateTime inProgressDataTime)
    {
        Debug.Assert(InstrumentViewModel.Instrument != null);
        Debug.Assert(InstrumentViewModel.MatchedDimensions != null);

        // Unit comes from the instrument and they're not localized.
        // The hardcoded "Count" label isn't localized for consistency.
        const string CountUnit = "Count";

        var unit = !InstrumentViewModel.ShowCount
            ? GetDisplayedUnit(InstrumentViewModel.Instrument)
            : CountUnit;

        List<Trace> traces;
        List<DateTime> xValues;
        if (InstrumentViewModel.Instrument.Type != OtlpInstrumentType.Histogram || InstrumentViewModel.ShowCount)
        {
            (traces, xValues) = CalculateChartValues(InstrumentViewModel.MatchedDimensions, GraphPointCount, tickUpdate, inProgressDataTime, unit);
        }
        else
        {
            (traces, xValues) = CalculateHistogramValues(InstrumentViewModel.MatchedDimensions, GraphPointCount, tickUpdate, inProgressDataTime, unit);
        }

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
                (inProgressDataTime - Duration).ToLocalTime()).ConfigureAwait(false);
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

    private string GetDisplayedUnit(OtlpInstrument instrument)
    {
        if (!string.IsNullOrEmpty(instrument.Unit))
        {
            var unit = OtlpUnits.GetUnit(instrument.Unit.TrimStart('{').TrimEnd('}'));
            return unit.Pluralize().Titleize();
        }

        // Hard code for instrument names that don't have units
        // but have a descriptive name that lets us infer the unit.
        if (instrument.Name.EndsWith(".count"))
        {
            return Loc[nameof(ControlsStrings.PlotlyChartCount)];
        }
        else if (instrument.Name.EndsWith(".length"))
        {
            return Loc[nameof(ControlsStrings.PlotlyChartLength)];
        }
        else
        {
            return Loc[nameof(ControlsStrings.PlotlyChartValue)];
        }
    }
}
