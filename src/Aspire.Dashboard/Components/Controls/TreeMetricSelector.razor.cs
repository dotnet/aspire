// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using MetricsResources = Aspire.Dashboard.Resources.Metrics;

namespace Aspire.Dashboard.Components.Controls;

public partial class TreeMetricSelector
{
    private readonly Dictionary<string, bool> _meterExpansion = new(StringComparer.Ordinal);

    // Cache keyed by instrument so we don't re-query the telemetry store on every render.
    // Cleared whenever the selected resource changes so stale "no data" results from a
    // previous resource aren't carried over.
    private readonly Dictionary<OtlpInstrumentKey, bool> _instrumentHasDataCache = new();

    private static readonly Icon s_gaugeIcon = new Icons.Regular.Size16.Gauge();
    private static readonly Icon s_counterIcon = new Icons.Regular.Size16.DataTrending();
    private static readonly Icon s_histogramIcon = new Icons.Regular.Size16.DataHistogram();

    private string _textFilter = string.Empty;

    [Parameter, EditorRequired]
    public required Func<Task> HandleSelectedTreeItemChangedAsync { get; set; }

    [Parameter, EditorRequired]
    public required Metrics.MetricsViewModel PageViewModel { get; set; }

    [Parameter]
    public bool IncludeLabel { get; set; }

    [Inject]
    public required DashboardDataSource DataSource { get; init; }

    [Inject]
    public required IInstrumentUnitResolver InstrumentUnitResolver { get; init; }

    [Inject]
    public required IStringLocalizer<MetricsResources> MetricsLoc { get; init; }

    public ITelemetryRepository TelemetryRepository => DataSource.TelemetryRepository;

    public void OnResourceChanged()
    {
        _meterExpansion.Clear();
        _instrumentHasDataCache.Clear();
        StateHasChanged();
    }

    // Lets the tree deemphasize instruments that haven't recorded any data yet, so the user
    // can tell at a glance which metrics are worth selecting instead of clicking through each one.
    private bool HasInstrumentData(OtlpInstrumentSummary instrument)
    {
        var key = instrument.GetKey();
        if (_instrumentHasDataCache.TryGetValue(key, out var hasData))
        {
            return hasData;
        }

        var resourceKey = PageViewModel.SelectedResource.Id?.GetResourceKey();
        hasData = resourceKey is { } rk
            && TelemetryRepository.GetInstrumentLatestEndTime(rk, instrument.Parent.Name, instrument.Name) is not null;

        _instrumentHasDataCache[key] = hasData;
        return hasData;
    }

    private string? GetSelectedTreeItemId()
    {
        if (PageViewModel.SelectedInstrument is { } instrument)
        {
            return GetInstrumentTreeItemId(instrument.Parent.Name, instrument.Name);
        }

        return PageViewModel.SelectedMeter is { } meterName ? GetMeterTreeItemId(meterName) : null;
    }

    private static string GetMeterTreeItemId(string meterName)
    {
        return $"metric-meter-{Uri.EscapeDataString(meterName)}";
    }

    private static string GetInstrumentTreeItemId(string meterName, string instrumentName)
    {
        return $"metric-instrument-{meterName.Length}-{Uri.EscapeDataString(meterName)}-{Uri.EscapeDataString(instrumentName)}";
    }

    private bool IsMeterExpanded(string meterName)
    {
        // Matches are always visible while filtering, otherwise a collapsed meter would hide the
        // instruments the user is searching for.
        if (_textFilter.Length > 0)
        {
            return true;
        }

        return _meterExpansion.TryGetValue(meterName, out var expanded)
            ? expanded
            : true;
    }

    private void SetMeterExpanded(string meterName, bool expanded)
    {
        // Don't record the forced expansion applied while filtering as the user's choice.
        if (_textFilter.Length > 0)
        {
            return;
        }

        _meterExpansion[meterName] = expanded;
    }

    private List<MeterGroup> GetFilteredMeterGroups()
    {
        var filter = _textFilter.Trim();
        var groups = new List<MeterGroup>();

        foreach (var meterGroup in PageViewModel.Instruments!.GroupBy(i => i.Parent.Name).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            // A meter name match keeps all of its instruments so searching "AspNetCore" shows the
            // whole meter. Otherwise only instruments matching by name or description are kept.
            var meterMatches = filter.Length == 0 || meterGroup.Key.Contains(filter, StringComparisons.UserTextSearch);
            var instruments = meterGroup
                .Where(i => meterMatches || InstrumentMatchesFilter(i, filter))
                .OrderBy(i => i.Name, StringComparer.Ordinal)
                .ToList();

            if (instruments.Count > 0)
            {
                groups.Add(new MeterGroup(meterGroup.Key, instruments));
            }
        }

        return groups;
    }

    private static bool InstrumentMatchesFilter(OtlpInstrumentSummary instrument, string filter)
    {
        return instrument.Name.Contains(filter, StringComparisons.UserTextSearch)
            || instrument.Description.Contains(filter, StringComparisons.UserTextSearch);
    }

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

    private string GetInstrumentTooltip(OtlpInstrumentSummary instrument, bool hasData)
    {
        // e.g. "http.server.request.duration — Histogram (milliseconds)"
        var typeName = instrument.Type switch
        {
            OtlpInstrumentType.Gauge => MetricsLoc[nameof(MetricsResources.MetricsInstrumentTypeGauge)].Value,
            OtlpInstrumentType.Sum => MetricsLoc[nameof(MetricsResources.MetricsInstrumentTypeCounter)].Value,
            OtlpInstrumentType.Histogram => MetricsLoc[nameof(MetricsResources.MetricsInstrumentTypeHistogram)].Value,
            _ => null
        };

        var tooltip = instrument.Name;
        if (typeName is not null)
        {
            var unit = InstrumentUnitResolver.ResolveDisplayedUnit(instrument, titleCase: false, pluralize: true);
            tooltip += $" \u2014 {typeName} ({unit})";
        }

        if (!hasData)
        {
            tooltip += $" \u2014 {MetricsLoc[nameof(MetricsResources.MetricsInstrumentNoDataTooltip)]}";
        }

        return tooltip;
    }

    private sealed record MeterGroup(string MeterName, List<OtlpInstrumentSummary> Instruments);
}
