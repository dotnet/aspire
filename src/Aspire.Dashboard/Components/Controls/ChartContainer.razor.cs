// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Components;

public partial class ChartContainer : ComponentBase, IAsyncDisposable
{
    private readonly CounterChartViewModel _viewModel = new();

    private OtlpInstrument? _instrument;
    private PeriodicTimer? _tickTimer;
    private Task? _tickTask;
    private IDisposable? _themeChangedSubscription;
    private int _renderedDimensionsCount;
    private string? _previousMeterName;
    private string? _previousInstrumentName;
    private bool _showCount;
    private readonly InstrumentViewModel _instrumentViewModel = new InstrumentViewModel();

    [Parameter, EditorRequired]
    public required string ApplicationId { get; set; }

    [Parameter, EditorRequired]
    public required string MeterName { get; set; }

    [Parameter, EditorRequired]
    public required string InstrumentName { get; set; }

    [Parameter, EditorRequired]
    public required TimeSpan Duration { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required ILogger<ChartContainer> Logger { get; set; }

    [Inject]
    public required ThemeManager ThemeManager { get; set; }

    protected override void OnInitialized()
    {
        // Update the graph every 200ms. This displays the latest data and moves time forward.
        _tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(0.2));
        _tickTask = Task.Run(UpdateDataAsync);
        _themeChangedSubscription = ThemeManager.OnThemeChanged(async () =>
        {
            _instrumentViewModel.Theme = ThemeManager.Theme;
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
            _instrument = GetInstrument();
            if (_instrument == null)
            {
                continue;
            }

            if (_instrument.Dimensions.Count > _renderedDimensionsCount)
            {
                // Re-render the entire control if the number of dimensions has changed.
                _renderedDimensionsCount = _instrument.Dimensions.Count;
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                // Only update data in plotly
                await UpdateInstrumentDataAsync(_instrument);
            }
        }
    }

    public async Task DimensionValuesChangedAsync(DimensionFilterViewModel dimensionViewModel)
    {
        if (_instrument == null)
        {
            return;
        }

        await UpdateInstrumentDataAsync(_instrument);
    }

    private async Task UpdateInstrumentDataAsync(OtlpInstrument instrument)
    {
        var matchedDimensions = instrument.Dimensions.Values.Where(MatchDimension).ToList();

        // Only update data in plotly
        await _instrumentViewModel.UpdateDataAsync(instrument, matchedDimensions);
    }

    private bool MatchDimension(DimensionScope dimension)
    {
        foreach (var dimensionFilter in _viewModel.DimensionFilters)
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
            return true;
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
        _instrument = GetInstrument();

        if (_instrument == null)
        {
            return;
        }

        var hasInstrumentChanged = _previousMeterName != MeterName || _previousInstrumentName != InstrumentName;
        _previousMeterName = MeterName;
        _previousInstrumentName = InstrumentName;

        var filters = CreateUpdatedFilters(hasInstrumentChanged);

        _viewModel.DimensionFilters.Clear();
        _viewModel.DimensionFilters.AddRange(filters);

        await UpdateInstrumentDataAsync(_instrument);
    }

    private OtlpInstrument? GetInstrument()
    {
        var instrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationServiceId = ApplicationId,
            MeterName = MeterName,
            InstrumentName = InstrumentName,
            StartTime = DateTime.UtcNow.Subtract(Duration),
            EndTime = DateTime.UtcNow,
        });

        if (instrument == null)
        {
            Logger.LogDebug(
                "Unable to find instrument. ApplicationServiceId: {ApplicationServiceId}, MeterName: {MeterName}, InstrumentName: {InstrumentName}",
                ApplicationId,
                MeterName,
                InstrumentName);
        }

        return instrument;
    }

    private List<DimensionFilterViewModel> CreateUpdatedFilters(bool hasInstrumentChanged)
    {
        var filters = new List<DimensionFilterViewModel>();
        if (_instrument != null)
        {
            foreach (var item in _instrument.KnownAttributeValues.OrderBy(kvp => kvp.Key))
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
                if (hasInstrumentChanged)
                {
                    // Select all by default.
                    item.SelectedValues = item.Values.ToList();
                }
                else
                {
                    var existing = _viewModel.DimensionFilters.SingleOrDefault(m => m.Name == item.Name);
                    if (existing != null)
                    {
                        // Select previously selected.
                        item.SelectedValues = item.Values.Where(newValue => existing.Values.Any(existingValue => existingValue.Name == newValue.Name)).ToList();
                    }
                    else
                    {
                        // No filter. Select none.
                        item.SelectedValues = new List<DimensionValueViewModel>();
                    }
                }
            }
        }

        return filters;
    }

    private void ShowCountChanged()
    {
        _instrumentViewModel.ShowCount = _showCount;
    }
}
