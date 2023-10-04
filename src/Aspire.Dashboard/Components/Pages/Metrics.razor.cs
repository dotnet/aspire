// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable
{
    private static readonly ApplicationViewModel s_selectApplication = new ApplicationViewModel { Id = null, Name = "Select service..." };

    private List<ApplicationViewModel> _applications = default!;
    private ApplicationViewModel _selectedApplication = s_selectApplication;
    private static readonly List<MetricsDurationViewModel> s_durations = new List<MetricsDurationViewModel>
    {
        new MetricsDurationViewModel { Text = "Last 1 minute", Duration = TimeSpan.FromMinutes(1) },
        new MetricsDurationViewModel { Text = "Last 5 minutes", Duration = TimeSpan.FromMinutes(5) },
        new MetricsDurationViewModel { Text = "Last 15 minutes", Duration = TimeSpan.FromMinutes(15) },
        new MetricsDurationViewModel { Text = "Last 30 minutes", Duration = TimeSpan.FromMinutes(30) },
        new MetricsDurationViewModel { Text = "Last 1 hour", Duration = TimeSpan.FromHours(1) },
        new MetricsDurationViewModel { Text = "Last 3 hours", Duration = TimeSpan.FromHours(3) },
        new MetricsDurationViewModel { Text = "Last 6 hours", Duration = TimeSpan.FromHours(6) },
        new MetricsDurationViewModel { Text = "Last 12 hours", Duration = TimeSpan.FromHours(12) },
        new MetricsDurationViewModel { Text = "Last 24 hours", Duration = TimeSpan.FromHours(24) },
    };
    private MetricsDurationViewModel _selectedDuration = s_durations[0];
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

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; set; }

    [Inject]
    public required ProtectedSessionStorage ProtectedSessionStore { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel ViewModel { get; set; }

    protected override Task OnInitializedAsync()
    {
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
        _selectedApplication = _applications.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? _applications.ElementAtOrDefault(1) ?? s_selectApplication;
        ViewModel.ApplicationServiceId = _selectedApplication.Id;
        _instruments = !string.IsNullOrEmpty(_selectedApplication.Id) ? TelemetryRepository.GetInstrumentsSummary(_selectedApplication.Id) : null;

        _selectedMeter = null;
        _selectedInstrument = null;
        if (_instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            _selectedMeter = _instruments.FirstOrDefault(i => i.Parent.MeterName == MeterName)?.Parent;
            if (_selectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                _selectedInstrument = TelemetryRepository.GetInstrument(ApplicationInstanceId!, MeterName, InstrumentName);
            }
        }

        UpdateSubscription();
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications().Select(a => new ApplicationViewModel { Id = a.InstanceId, Name = a.ApplicationName }).ToList();
        _applications.Insert(0, s_selectApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        NavigationManager.NavigateTo($"/Metrics/{_selectedApplication.Id}");
        return Task.CompletedTask;
    }

    private static Task HandleSelectedDurationChangedAsync()
    {
        return Task.CompletedTask;
    }

    private sealed class MetricsSelectedState
    {
        public const string Key = "Metrics_SelectState";
        public string? ApplicationId { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
    }

    private async Task HandleSelectedTreeItemChanged()
    {
        MetricsSelectedState? state = null;

        if (_selectedTreeItem?.Data is OtlpMeter meter)
        {
            state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, MeterName = meter.MeterName };
        }
        else if (_selectedTreeItem?.Data is OtlpInstrument instrument)
        {
            state = new MetricsSelectedState { ApplicationId = _selectedApplication.Id, MeterName = instrument.Parent.MeterName, InstrumentName = instrument.Name };
        }

        if (state != null)
        {
            NavigateTo(state);
            await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
        }
        else
        {
            await ProtectedSessionStore.DeleteAsync(MetricsSelectedState.Key);
        }
    }

    private void NavigateTo(MetricsSelectedState state)
    {
        if (state.MeterName != null)
        {
            if (state.InstrumentName != null)
            {
                NavigationManager.NavigateTo($"/Metrics/{state.ApplicationId}/Meter/{state.MeterName}/Instrument/{state.InstrumentName}");
            }
            else
            {
                NavigationManager.NavigateTo($"/Metrics/{state.ApplicationId}/Meter/{state.MeterName}");
            }
        }
        else
        {
            NavigationManager.NavigateTo($"/Metrics/{state.ApplicationId}");
        }
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_metricsSubscription is null || _metricsSubscription.ApplicationId != _selectedApplication.Id)
        {
            _metricsSubscription?.Dispose();
            _metricsSubscription = TelemetryRepository.OnNewMetrics(_selectedApplication.Id, async () =>
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
