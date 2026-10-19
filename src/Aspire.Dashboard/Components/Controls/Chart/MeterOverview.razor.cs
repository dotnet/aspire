// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using MetricsResources = Aspire.Dashboard.Resources.Metrics;

namespace Aspire.Dashboard.Components;

/// <summary>
/// Shows every instrument of a meter as a card with its latest value and a sparkline,
/// so the user can scan a meter at a glance and jump to the instrument worth investigating.
/// </summary>
public partial class MeterOverview : ComponentBase, IAsyncDisposable
{
    // Sparklines show the trend shape, not detail, so fewer points than the full chart is enough.
    private const int SparklinePointCount = 30;
    internal const double SparklineWidth = 100;
    internal const double SparklineHeight = 32;
    private static readonly string s_sparklineBaselineY = (SparklineHeight - SparklinePath.Inset).ToString(CultureInfo.InvariantCulture);

    // Cards refresh less often than the main chart: a meter can have dozens of instruments
    // and each refresh queries the telemetry store once per instrument.
    private static readonly TimeSpan s_refreshInterval = TimeSpan.FromSeconds(3);

    private static readonly Icon s_gaugeIcon = new Icons.Regular.Size16.Gauge();
    private static readonly Icon s_counterIcon = new Icons.Regular.Size16.DataTrending();
    private static readonly Icon s_histogramIcon = new Icons.Regular.Size16.DataHistogram();

    private static int s_nextOverviewId;

    private readonly CancellationTokenSource _disposeCts = new();
    // Copied from the CTS so it stays readable after the CTS is disposed. A parameter update can
    // race with disposal when the user switches resource, and CancellationTokenSource.Token throws
    // ObjectDisposedException once disposed.
    private readonly CancellationToken _disposeToken;
    private readonly string _gradientIdPrefix = $"meter-overview-{Interlocked.Increment(ref s_nextOverviewId)}";
    private Dictionary<OtlpInstrumentKey, InstrumentCardData> _cardData = new();
    private Task? _refreshTask;
    private (ResourceKey, TimeSpan, string)? _loadedParameters;

    [Parameter, EditorRequired]
    public required ResourceKey ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets whether cards show their meter name, for groups mixing instruments from several meters.
    /// </summary>
    [Parameter, EditorRequired]
    public required bool ShowMeterName { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<OtlpInstrumentSummary> Instruments { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    [Parameter, EditorRequired]
    public required Func<OtlpInstrumentSummary, string> GetInstrumentUrl { get; set; }

    [Inject]
    public required DashboardDataSource DataSource { get; init; }

    [Inject]
    public required IInstrumentUnitResolver InstrumentUnitResolver { get; init; }

    [Inject]
    public required PauseManager PauseManager { get; init; }

    [Inject]
    public required ILogger<MeterOverview> Logger { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    private ITelemetryRepository TelemetryRepository => DataSource.TelemetryRepository;

    public MeterOverview()
    {
        _disposeToken = _disposeCts.Token;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_disposeToken.IsCancellationRequested)
        {
            return;
        }

        // Compare the instruments themselves, not just their count: unpinning one instrument and pinning another
        // keeps the pinned group's count but changes which cards need data.
        var parameters = (ResourceKey, Duration, string.Join('\n', Instruments.Select(i => $"{i.Parent.Name}\t{i.Name}")));
        if (_loadedParameters != parameters)
        {
            _loadedParameters = parameters;
            try
            {
                await RefreshAsync(_disposeToken);
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                return;
            }
        }

        _refreshTask ??= RefreshPeriodicallyAsync(_disposeToken);
    }

    private async Task RefreshPeriodicallyAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(s_refreshInterval, TimeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                // Read-only telemetry doesn't change, and paused metrics should stay frozen.
                if (TelemetryRepository.IsReadOnly || PauseManager.AreMetricsPaused(out _))
                {
                    continue;
                }

                await RefreshAsync(cancellationToken);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Keep a background failure from surfacing as an unobserved error during disposal.
            Logger.LogError(ex, "Unexpected error refreshing meter overview.");
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var cardData = new Dictionary<OtlpInstrumentKey, InstrumentCardData>();
        foreach (var instrument in Instruments)
        {
            cardData[instrument.GetKey()] = await GetCardDataAsync(instrument, cancellationToken);
        }

        // Swap the whole dictionary so a render never sees a partially refreshed set of cards.
        _cardData = cardData;
    }

    private async Task<InstrumentCardData> GetCardDataAsync(OtlpInstrumentSummary instrument, CancellationToken cancellationToken)
    {
        DateTime endTime;
        if (TelemetryRepository.IsReadOnly)
        {
            // Imported telemetry is historical, so anchor each sparkline to that instrument's last data.
            var latestEndTime = TelemetryRepository.GetInstrumentLatestEndTime(ResourceKey, instrument.Parent.Name, instrument.Name);
            if (latestEndTime is null)
            {
                return InstrumentCardData.Empty;
            }
            endTime = latestEndTime.Value;
        }
        else
        {
            endTime = PauseManager.AreMetricsPaused(out var pausedAt)
                ? pausedAt.Value
                // Compensate for delay in receiving metrics from services, matching the main chart.
                : TimeProvider.GetUtcNow().UtcDateTime.Subtract(TimeSpan.FromSeconds(1));
        }

        var dataPointInterval = MetricDataPointInterval.Get(Duration);

        // Histograms need one preceding rollup to calculate bucket count changes at the start of the range.
        var historyDuration = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(30).Ticks, dataPointInterval.Ticks));
        var data = await TelemetryRepository.GetInstrumentAsync(new GetInstrumentRequest
        {
            ResourceKey = ResourceKey,
            MeterName = instrument.Parent.Name,
            InstrumentName = instrument.Name,
            StartTime = endTime.Subtract(Duration + historyDuration),
            EndTime = endTime,
            DataPointInterval = dataPointInterval,
            IncludeExemplars = false,
            PopulateExemplarAttributes = false
        }, cancellationToken).ConfigureAwait(false);

        if (data is null)
        {
            return InstrumentCardData.Empty;
        }

        var calculator = new ChartDataCalculator(SparklinePointCount, Duration);
        var endTimeOffset = new DateTimeOffset(endTime, TimeSpan.Zero);
        var unit = InstrumentUnitResolver.ResolveDisplayedUnit(instrument, titleCase: true, pluralize: true);

        // Histograms show their median. It's the most representative single value for a
        // distribution, and the full chart is one click away for P90/P99.
        var chartData = instrument.Type == OtlpInstrumentType.Histogram
            ? calculator.CalculateHistogramValues(data.Dimensions, endTimeOffset, static t => t, unit)
            : calculator.CalculateChartValues(data.Dimensions, endTimeOffset, static t => t, unit);
        var trace = chartData.Traces.Single(t => t.Percentile is null or 50);

        // The calculator pads points past the end time so the full chart's line reaches the right
        // edge. Those buckets are still filling up (or empty), which shows as a misleading drop at
        // the end of a sparkline, so only keep points that end by the end time.
        var values = new List<double?>(trace.Values.Count);
        for (var i = 0; i < trace.Values.Count; i++)
        {
            if (chartData.XValues[i] <= endTimeOffset)
            {
                values.Add(trace.Values[i]);
            }
        }
        var latestValue = values.LastOrDefault(v => v is not null);
        if (latestValue is null)
        {
            return InstrumentCardData.Empty;
        }

        // The form follows what the reader wants from each instrument type:
        // - Counters only ever grow, so a line of the running total says little. Bars of the
        //   increase per interval show the actual activity, e.g. requests per interval.
        // - Histograms are often sparse (no requests means no data), and a line drawn between rare
        //   points implies values that were never measured. A bar per interval with data doesn't.
        // - Gauges and up-down counters are levels, which is what a line shows best.
        if (instrument.Type == OtlpInstrumentType.Sum && instrument.IsMonotonic)
        {
            var increases = CalculateCounterIncreases(values, instrument.AggregationTemporality);
            return new InstrumentCardData(latestValue, SparklineKind.Bars, SparklinePath.BuildBars(increases, SparklineWidth, SparklineHeight), string.Empty);
        }

        if (instrument.Type == OtlpInstrumentType.Histogram)
        {
            return new InstrumentCardData(latestValue, SparklineKind.Bars, SparklinePath.BuildBars(values, SparklineWidth, SparklineHeight), string.Empty);
        }

        var (line, area) = SparklinePath.Build(values, SparklineWidth, SparklineHeight);
        return new InstrumentCardData(latestValue, SparklineKind.Line, line, area);
    }

    internal static List<double?> CalculateCounterIncreases(IReadOnlyList<double?> values, OtlpAggregationTemporality temporality)
    {
        // Delta counters already report the change per export, so their values are the increases.
        if (temporality == OtlpAggregationTemporality.Delta)
        {
            return [.. values];
        }

        // Cumulative counters report a running total, e.g. [10, 15, null, 22, 3] where 3 follows a
        // process restart. The increase is the difference from the last known total, carried across
        // empty intervals: an app exporting every 60s leaves most 10s intervals empty, and its
        // growth should show up in the interval where it was observed, not be dropped.
        // After a reset the total starts again from zero, so the new value itself is the increase.
        var increases = new List<double?>(values.Count);
        double? previous = null;
        foreach (var value in values)
        {
            if (value is not { } current)
            {
                increases.Add(null);
                continue;
            }

            // The first point has nothing to compare against.
            increases.Add(previous is { } last ? (current >= last ? current - last : current) : null);
            previous = current;
        }

        return increases;
    }

    private InstrumentCardData GetCardData(OtlpInstrumentSummary instrument)
    {
        return _cardData.TryGetValue(instrument.GetKey(), out var data) ? data : InstrumentCardData.Empty;
    }

    private string FormatUnit(OtlpInstrumentSummary instrument, double value)
    {
        return InstrumentUnitResolver.ResolveDisplayedUnit(instrument, titleCase: false, pluralize: value != 1);
    }

    internal static string FormatCompactValue(double value, CultureInfo culture)
    {
        // Cards have limited room, so large values use a short suffix, e.g. 125300000 -> "125.3M".
        // Small values keep their decimals because durations such as 0.004 seconds are meaningful.
        var abs = Math.Abs(value);
        var (scaled, suffix) = abs switch
        {
            >= 1e12 => (value / 1e12, "T"),
            >= 1e9 => (value / 1e9, "B"),
            >= 1e6 => (value / 1e6, "M"),
            >= 1e4 => (value / 1e3, "K"),
            _ => (value, string.Empty)
        };

        return suffix.Length > 0
            ? FormatHelpers.FormatNumberWithOptionalDecimalPlaces(scaled, maxDecimalPlaces: 1, culture) + suffix
            : FormatHelpers.FormatNumberWithOptionalDecimalPlaces(scaled, maxDecimalPlaces: 3, culture);
    }

    private string? GetInstrumentTypeName(OtlpInstrumentType type) => type switch
    {
        OtlpInstrumentType.Gauge => Loc[nameof(MetricsResources.MetricsInstrumentTypeGauge)].Value,
        OtlpInstrumentType.Sum => Loc[nameof(MetricsResources.MetricsInstrumentTypeCounter)].Value,
        OtlpInstrumentType.Histogram => Loc[nameof(MetricsResources.MetricsInstrumentTypeHistogram)].Value,
        _ => null
    };

    private static Icon? GetInstrumentIcon(OtlpInstrumentType type)
    {
        return type switch
        {
            OtlpInstrumentType.Gauge => s_gaugeIcon,
            OtlpInstrumentType.Sum => s_counterIcon,
            OtlpInstrumentType.Histogram => s_histogramIcon,
            _ => null
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _disposeCts.CancelAsync();
        if (_refreshTask is { } task)
        {
            await task;
        }
        _disposeCts.Dispose();
    }

    private enum SparklineKind
    {
        Line,
        Bars
    }

    // For lines, Path is the stroke and AreaPath the fill below it. For bars, Path holds all bars.
    private sealed record InstrumentCardData(double? LatestValue, SparklineKind Kind, string Path, string AreaPath)
    {
        public static readonly InstrumentCardData Empty = new(null, SparklineKind.Line, string.Empty, string.Empty);
    }
}
