// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public abstract class ChartContainer : ComponentBase, IAsyncDisposable
{
    private OtlpInstrumentData? _instrument;
    private PeriodicTimer? _tickTimer;
    private Task? _tickTask;
    private IDisposable? _themeChangedSubscription;
    private int _renderedDimensionsCount;

    [Parameter, EditorRequired]
    public required ApplicationKey ApplicationKey { get; set; }

    [Parameter, EditorRequired]
    public required string MeterName { get; set; }

    [Parameter, EditorRequired]
    public required string InstrumentName { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    [Parameter, EditorRequired]
    public required Func<Metrics.MetricViewKind, Task> OnViewChangedAsync { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required ILogger<ChartContainer> Logger { get; init; }

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    public List<DimensionFilterViewModel> DimensionFilters { get; } = [];
    public string? PreviousMeterName { get; set; }
    public string? PreviousInstrumentName { get; set; }
    public OtlpInstrument? Instrument { get; private set; }
    public InstrumentViewModel ViewModel { get; } = new InstrumentViewModel();

    protected override void OnInitialized()
    {
        // Update the graph every 200ms. This displays the latest data and moves time forward.
        _tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(0.2));
        _tickTask = Task.Run(UpdateDataAsync);
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            ViewModel.Theme = ThemeManager.Theme;
            await InvokeAsync(StateHasChanged);
        });
    }

    public async ValueTask DisposeAsync()
    {
        _themeChangedSubscription?.Dispose();
        _tickTimer?.Dispose();

        // Wait for UpdateData to complete.
        if (_tickTask is { } t)
        {
            await t;
        }
    }

    private async Task UpdateDataAsync()
    {
        var timer = _tickTimer;
        while (await timer!.WaitForNextTickAsync())
        {
            Instrument = GetInstrument();
            if (Instrument == null)
            {
                continue;
            }

            if (Instrument.Dimensions.Count > _renderedDimensionsCount)
            {
                // Re-render the entire control if the number of dimensions has changed.
                _renderedDimensionsCount = Instrument.Dimensions.Count;
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                await UpdateInstrumentDataAsync(Instrument);
            }
        }
    }

    public async Task DimensionValuesChangedAsync(DimensionFilterViewModel dimensionViewModel)
    {
        if (Instrument == null)
        {
            return;
        }

        await UpdateInstrumentDataAsync(Instrument);
    }

    private async Task UpdateInstrumentDataAsync(OtlpInstrumentData instrument)
    {
        var matchedDimensions = instrument.Dimensions.Where(MatchDimension).ToList();

        // Only update data in plotly
        await _instrumentViewModel.UpdateDataAsync(instrument, matchedDimensions);
    }

    private bool MatchDimension(DimensionScope dimension)
    {
        foreach (var dimensionFilter in DimensionFilters)
        {
            if (!MatchFilter(dimension.Attributes, dimensionFilter))
            {
                return false;
            }
        }
        return true;
    }

    private static bool MatchFilter(KeyValuePair<string, string>[] attributes, DimensionFilterViewModel filter)
    {
        // No filter selected.
        if (!filter.SelectedValues.Any())
        {
            return false;
        }

        var value = OtlpHelpers.GetValue(attributes, filter.Name);
        foreach (var item in filter.SelectedValues)
        {
            if (item.Empty && string.IsNullOrEmpty(value))
            {
                return true;
            }
            if (item.Name == value)
            {
                return true;
            }
        }

        return false;
    }

    protected override async Task OnParametersSetAsync()
    {
        Instrument = GetInstrument();

        if (Instrument == null)
        {
            return;
        }

        var hasInstrumentChanged = PreviousMeterName != MeterName || PreviousInstrumentName != InstrumentName;
        PreviousMeterName = MeterName;
        PreviousInstrumentName = InstrumentName;

        var filters = CreateUpdatedFilters(hasInstrumentChanged);

        DimensionFilters.Clear();
        DimensionFilters.AddRange(filters);

        await UpdateInstrumentDataAsync(Instrument);
    }

    private OtlpInstrumentData? GetInstrument()
    {
        var endDate = DateTime.UtcNow;
        // Get more data than is being displayed. Histogram graph uses some historical data to calculate bucket counts.
        // It's ok to get more data than is needed here. An additional date filter is applied when building chart values.
        var startDate = endDate.Subtract(Duration + TimeSpan.FromSeconds(30));

        var instrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = ApplicationKey,
            MeterName = MeterName,
            InstrumentName = InstrumentName,
            StartTime = startDate,
            EndTime = endDate,
        });

        if (instrument == null)
        {
            Logger.LogDebug(
                "Unable to find instrument. ApplicationKey: {ApplicationKey}, MeterName: {MeterName}, InstrumentName: {InstrumentName}",
                ApplicationKey,
                MeterName,
                InstrumentName);
        }

        return instrument;
    }

    private List<DimensionFilterViewModel> CreateUpdatedFilters(bool hasInstrumentChanged)
    {
        var filters = new List<DimensionFilterViewModel>();
        if (Instrument != null)
        {
            foreach (var item in Instrument.KnownAttributeValues.OrderBy(kvp => kvp.Key))
            {
                var dimensionModel = new DimensionFilterViewModel
                {
                    Name = item.Key
                };

                dimensionModel.Values.AddRange(item.Value.OrderBy(v => v).Select(v =>
                {
                    var empty = string.IsNullOrEmpty(v);
                    return new DimensionValueViewModel
                    {
                        Name = empty ? "(Empty)" : v,
                        Empty = empty
                    };
                }));

                filters.Add(dimensionModel);
            }

            foreach (var item in filters)
            {
                item.SelectedValues.Clear();

                if (hasInstrumentChanged)
                {
                    // Select all by default.
                    foreach (var v in item.Values)
                    {
                        item.SelectedValues.Add(v);
                    }
                }
                else
                {
                    var existing = DimensionFilters.SingleOrDefault(m => m.Name == item.Name);
                    if (existing != null)
                    {
                        // Select previously selected.
                        // Automatically select new incoming values if existing values are all selected.
                        var newSelectedValues = (existing.AreAllValuesSelected ?? false)
                            ? item.Values
                            : item.Values.Where(newValue => existing.SelectedValues.Any(existingValue => existingValue.Name == newValue.Name));

                        foreach (var v in newSelectedValues)
                        {
                            item.SelectedValues.Add(v);
                        }
                    }
                    else
                    {
                        // New filter. Select all by default.
                        foreach (var v in item.Values)
                        {
                            item.SelectedValues.Add(v);
                        }
                    }
                }
            }
        }

        return filters;
    }
}
