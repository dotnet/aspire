// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable
{
    private static readonly SelectViewModel<string> s_selectApplication = new SelectViewModel<string> { Id = null, Name = "(Select a resource)" };
    private List<SelectViewModel<TimeSpan>> _durations = null!;
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);

    private List<SelectViewModel<string>> _applications = default!;
    private SelectViewModel<string> _selectedApplication = s_selectApplication;
    private SelectViewModel<TimeSpan> _selectedDuration = null!;
    private Subscription? _applicationsSubscription;
    private Subscription? _metricsSubscription;
    private List<OtlpInstrument>? _instruments;
    private FluentTreeItem? _selectedTreeItem;
    private OtlpMeter? _selectedMeter;
    private OtlpInstrument? _selectedInstrument;

    [Parameter]
    public string? ApplicationInstanceId { get; set; }

    [Parameter]
    public string? MeterName { get; set; }

    [Parameter]
    public string? InstrumentName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "duration")]
    public int DurationMinutes { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IResourceService ResourceService { get; set; }

    [Inject]
    public required ProtectedSessionStorage ProtectedSessionStore { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel ViewModel { get; set; }

    protected override Task OnInitializedAsync()
    {
        _durations = new List<SelectViewModel<TimeSpan>>
        {
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastOneMinute], Id = TimeSpan.FromMinutes(1) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastFiveMinutes], Id = TimeSpan.FromMinutes(5) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastFifteenMinutes], Id = TimeSpan.FromMinutes(15) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastThirtyMinutes], Id = TimeSpan.FromMinutes(30) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastHour], Id = TimeSpan.FromHours(1) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastThreeHours], Id = TimeSpan.FromHours(3) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastSixHours], Id = TimeSpan.FromHours(6) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastTwelveHours], Id = TimeSpan.FromHours(12) },
            new() { Name = Loc[Dashboard.Resources.Metrics.MetricsLastTwentyFourHours], Id = TimeSpan.FromHours(24) },
        };

        _selectedDuration = _durations.Single(d => d.Id == s_defaultDuration);

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(() => InvokeAsync(() =>
        {
            UpdateApplications();
            StateHasChanged();
        }));
        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        _selectedDuration = _durations.SingleOrDefault(d => (int)d.Id.TotalMinutes == DurationMinutes) ?? _durations.Single(d => d.Id == s_defaultDuration);
        _selectedApplication = _applications.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? s_selectApplication;
        ViewModel.ApplicationServiceId = _selectedApplication.Id;
        _instruments = !string.IsNullOrEmpty(_selectedApplication.Id) ? TelemetryRepository.GetInstrumentsSummary(_selectedApplication.Id) : null;

        _selectedMeter = null;
        _selectedInstrument = null;
        if (_instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            _selectedMeter = _instruments.FirstOrDefault(i => i.Parent.MeterName == MeterName)?.Parent;
            if (_selectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                _selectedInstrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
                {
                    ApplicationServiceId = ApplicationInstanceId!,
                    MeterName = MeterName,
                    InstrumentName = InstrumentName
                });
            }
        }

        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = SelectViewModelFactory.CreateApplicationsSelectViewModel(TelemetryRepository.GetApplications());
        _applications.Insert(0, s_selectApplication);
        UpdateSubscription();
    }

    private async Task HandleSelectedApplicationChangedAsync()
    {
        var state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, DurationMinutes = (int)_selectedDuration.Id.TotalMinutes };

        NavigateTo(state);
        await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
    }

    private async Task HandleSelectedDurationChangedAsync()
    {
        var state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, DurationMinutes = (int)_selectedDuration.Id.TotalMinutes, InstrumentName = InstrumentName, MeterName = MeterName };

        NavigateTo(state);
        await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
    }

    private sealed class MetricsSelectedState
    {
        public const string Key = "Metrics_SelectState";
        public string? ApplicationId { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
        public int DurationMinutes { get; set; }
    }

    private async Task HandleSelectedTreeItemChanged()
    {
        MetricsSelectedState state;

        if (_selectedTreeItem?.Data is OtlpMeter meter)
        {
            state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, DurationMinutes = (int)_selectedDuration.Id.TotalMinutes, MeterName = meter.MeterName };
        }
        else if (_selectedTreeItem?.Data is OtlpInstrument instrument)
        {
            state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, DurationMinutes = (int)_selectedDuration.Id.TotalMinutes, MeterName = instrument.Parent.MeterName, InstrumentName = instrument.Name };
        }
        else
        {
            state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, DurationMinutes = (int)_selectedDuration.Id.TotalMinutes };
        }

        NavigateTo(state);
        await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
    }

    private void NavigateTo(MetricsSelectedState state)
    {
        string url;
        if (state.MeterName != null)
        {
            if (state.InstrumentName != null)
            {
                url = $"/Metrics/{state.ApplicationId}/Meter/{state.MeterName}/Instrument/{state.InstrumentName}";
            }
            else
            {
                url = $"/Metrics/{state.ApplicationId}/Meter/{state.MeterName}";
            }
        }
        else if (state.ApplicationId != null)
        {
            url = $"/Metrics/{state.ApplicationId}";
        }
        else
        {
            url = $"/Metrics";
        }

        if (state.DurationMinutes != (int)s_defaultDuration.TotalMinutes)
        {
            url += $"?duration={state.DurationMinutes}";
        }

        NavigationManager.NavigateTo(url);
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_metricsSubscription is null || _metricsSubscription.ApplicationId != _selectedApplication.Id)
        {
            _metricsSubscription?.Dispose();
            _metricsSubscription = TelemetryRepository.OnNewMetrics(_selectedApplication.Id, SubscriptionType.Read, async () =>
            {
                var selectedApplicationId = _selectedApplication.Id;
                if (!string.IsNullOrEmpty(selectedApplicationId))
                {
                    // If there are more instruments than before then update the UI.
                    var instruments = TelemetryRepository.GetInstrumentsSummary(selectedApplicationId);

                    if (_instruments is null || instruments.Count > _instruments.Count)
                    {
                        _instruments = instruments;
                        await InvokeAsync(StateHasChanged);
                    }
                }
            });
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var result = await ProtectedSessionStore.GetAsync<MetricsSelectedState>(MetricsSelectedState.Key);
            if (result.Success && result.Value is not null)
            {
                NavigateTo(result.Value);
            }
        }
    }

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _metricsSubscription?.Dispose();
    }
}
