// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Web;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Components.Controls.Chart;

public abstract class ChartBase : ComponentBase
{
    private const int GraphPointCount = 30;

    private TimeSpan _tickDuration;
    private DateTimeOffset _lastUpdateTime;
    private DateTimeOffset _currentDataStartTime;
    private List<KeyValuePair<string, string>[]>? _renderedDimensionAttributes;
    private OtlpInstrumentKey? _renderedInstrument;
    private string? _renderedTheme;
    private bool _renderedShowCount;

    [Inject]
    public required IStringLocalizer<ControlsStrings> Loc { get; init; }

    [Inject]
    public required IInstrumentUnitResolver InstrumentUnitResolver { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Parameter, EditorRequired]
    public required InstrumentViewModel InstrumentViewModel { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    protected override void OnInitialized()
    {
        _currentDataStartTime = GetCurrentDataTime();
        InstrumentViewModel.DataUpdateSubscriptions.Add(OnInstrumentDataUpdate);
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
        else if (_lastUpdateTime.Add(TimeSpan.FromSeconds(0.2)) < TimeProvider.GetUtcNow())
        {
            // Throttle how often the chart is updated.
            _lastUpdateTime = TimeProvider.GetUtcNow();
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

    private (List<ChartTrace> Y, List<DateTimeOffset> X) CalculateHistogramValues(List<DimensionScope> dimensions, int pointCount, bool tickUpdate, DateTimeOffset inProgressDataTime, string yLabel)
    {
        var pointDuration = Duration / pointCount;
        var traces = new Dictionary<int, ChartTrace>
        {
            [50] = new() { Name = $"P50 {yLabel}", Percentile = 50 },
            [90] = new() { Name = $"P90 {yLabel}", Percentile = 90 },
            [99] = new() { Name = $"P99 {yLabel}", Percentile = 99 }
        };
        var xValues = new List<DateTimeOffset>();
        var startDate = _currentDataStartTime;
        DateTimeOffset? firstPointEndTime = null;

        // Generate the points in reverse order so that the chart is drawn from right to left.
        // Add a couple of extra points to the end so that the chart is drawn all the way to the right edge.
        for (var pointIndex = 0; pointIndex < (pointCount + 2); pointIndex++)
        {
            var start = CalcOffset(pointIndex, startDate, pointDuration);
            var end = CalcOffset(pointIndex - 1, startDate, pointDuration);
            firstPointEndTime ??= end;

            xValues.Add(TimeProvider.ToLocalDateTimeOffset(end));

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
            xValues.Add(TimeProvider.ToLocalDateTimeOffset(inProgressDataTime));
        }

        ChartTrace? previousValues = null;
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

    private string FormatTooltip(string name, double yValue, DateTimeOffset xValue)
    {
        return $"<b>{HttpUtility.HtmlEncode(InstrumentViewModel.Instrument?.Name)}</b><br />{HttpUtility.HtmlEncode(name)}: {FormatHelpers.FormatNumberWithOptionalDecimalPlaces(yValue, CultureInfo.CurrentCulture)}<br />Time: {FormatHelpers.FormatTime(TimeProvider, TimeProvider.ToLocal(xValue))}";
    }

    private static HistogramValue GetHistogramValue(MetricValueBase metric)
    {
        if (metric is HistogramValue histogramValue)
        {
            return histogramValue;
        }

        throw new InvalidOperationException("Unexpected metric type: " + metric.GetType());
    }

    internal static bool TryCalculateHistogramPoints(List<DimensionScope> dimensions, DateTimeOffset start, DateTimeOffset end, Dictionary<int, ChartTrace> traces)
    {
        var hasValue = false;

        ulong[]? currentBucketCounts = null;
        double[]? explicitBounds = null;

        start = start.Subtract(TimeSpan.FromSeconds(1));
        end = end.Add(TimeSpan.FromSeconds(1));

        foreach (var dimension in dimensions)
        {
            var dimensionValues = dimension.Values;
            for (var i = dimensionValues.Count - 1; i >= 0; i--)
            {
                var metric = dimensionValues[i];
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

                    var previousHistogramValues = i > 0 ? GetHistogramValue(dimensionValues[i - 1]).Values : null;

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
                            // Histogram values are cumulative, so subtract the previous value to get the diff.
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

    internal static double? CalculatePercentile(int percentile, ulong[] counts, double[] explicitBounds)
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

    private (List<ChartTrace> Y, List<DateTimeOffset> X) CalculateChartValues(List<DimensionScope> dimensions, int pointCount, bool tickUpdate, DateTimeOffset inProgressDataTime, string yLabel)
    {
        var pointDuration = Duration / pointCount;
        var yValues = new List<double?>();
        var xValues = new List<DateTimeOffset>();
        var startDate = _currentDataStartTime;
        DateTimeOffset? firstPointEndTime = null;

        // Generate the points in reverse order so that the chart is drawn from right to left.
        // Add a couple of extra points to the end so that the chart is drawn all the way to the right edge.
        for (var pointIndex = 0; pointIndex < (pointCount + 2); pointIndex++)
        {
            var start = CalcOffset(pointIndex, startDate, pointDuration);
            var end = CalcOffset(pointIndex - 1, startDate, pointDuration);
            firstPointEndTime ??= end;

            xValues.Add(TimeProvider.ToLocalDateTimeOffset(end));

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
            xValues.Add(TimeProvider.ToLocalDateTimeOffset(inProgressDataTime));
        }

        var trace = new ChartTrace
        {
            Name = HttpUtility.HtmlEncode(yLabel)
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

    private static bool TryCalculatePoint(List<DimensionScope> dimensions, DateTimeOffset start, DateTimeOffset end, out double pointValue)
    {
        var hasValue = false;
        pointValue = 0d;

        foreach (var dimension in dimensions)
        {
            var dimensionValues = dimension.Values;
            var dimensionValue = 0d;
            for (var i = dimensionValues.Count - 1; i >= 0; i--)
            {
                var metric = dimensionValues[i];
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

    private static DateTimeOffset CalcOffset(int pointIndex, DateTimeOffset now, TimeSpan pointDuration)
    {
        return now.Subtract(pointDuration * pointIndex);
    }

    private async Task UpdateChart(bool tickUpdate, DateTimeOffset inProgressDataTime)
    {
        // Unit comes from the instrument and they're not localized.
        // The hardcoded "Count" label isn't localized for consistency.
        const string CountUnit = "Count";

        Debug.Assert(InstrumentViewModel.MatchedDimensions != null);
        Debug.Assert(InstrumentViewModel.Instrument != null);

        var unit = !InstrumentViewModel.ShowCount
            ? GetDisplayedUnit(InstrumentViewModel.Instrument)
            : CountUnit;

        List<ChartTrace> traces;
        List<DateTimeOffset> xValues;
        if (InstrumentViewModel.Instrument?.Type != OtlpInstrumentType.Histogram || InstrumentViewModel.ShowCount)
        {
            (traces, xValues) = CalculateChartValues(InstrumentViewModel.MatchedDimensions, GraphPointCount, tickUpdate, inProgressDataTime, unit);
        }
        else
        {
            (traces, xValues) = CalculateHistogramValues(InstrumentViewModel.MatchedDimensions, GraphPointCount, tickUpdate, inProgressDataTime, unit);
        }

        await OnChartUpdated(traces, xValues, tickUpdate, inProgressDataTime);
    }

    private DateTimeOffset GetCurrentDataTime()
    {
        return TimeProvider.GetUtcNow().Subtract(TimeSpan.FromSeconds(1)); // Compensate for delay in receiving metrics from services.
    }

    private string GetDisplayedUnit(OtlpInstrument instrument)
    {
        return InstrumentUnitResolver.ResolveDisplayedUnit(instrument);
    }

    protected abstract Task OnChartUpdated(List<ChartTrace> traces, List<DateTimeOffset> xValues, bool tickUpdate, DateTimeOffset inProgressDataTime);
}
