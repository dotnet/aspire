// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable, IPageWithSessionAndUrlState<Metrics.MetricsViewModel, Metrics.MetricsPageState>
{
    private SelectViewModel<string> _selectApplication = null!;
    private List<SelectViewModel<TimeSpan>> _durations = null!;
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);

    private List<SelectViewModel<string>> _applications = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _metricsSubscription;

    public string BasePath => "Metrics";
    public string SessionStorageKey => "Metrics_PageState";
    public MetricsViewModel ViewModel { get; set; } = null!;

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
    public required ProtectedSessionStorage SessionStorage { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel TracesViewModel { get; set; }

    protected override Task OnInitializedAsync()
    {
        _durations = new List<SelectViewModel<TimeSpan>>
        {
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastOneMinute)], Id = TimeSpan.FromMinutes(1) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastFiveMinutes)], Id = TimeSpan.FromMinutes(5) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastFifteenMinutes)], Id = TimeSpan.FromMinutes(15) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastThirtyMinutes)], Id = TimeSpan.FromMinutes(30) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastHour)], Id = TimeSpan.FromHours(1) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastThreeHours)], Id = TimeSpan.FromHours(3) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastSixHours)], Id = TimeSpan.FromHours(6) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastTwelveHours)], Id = TimeSpan.FromHours(12) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastTwentyFourHours)], Id = TimeSpan.FromHours(24) },
        };

        _selectApplication = new SelectViewModel<string> { Id = null, Name = ControlsStringsLoc[ControlsStrings.SelectAResource] };
        ViewModel = new MetricsViewModel { SelectedApplication = _selectApplication, SelectedDuration = _durations.Single(d => d.Id == s_defaultDuration) };

        UpdateApplications();
        _applicationsSubscription = TelemetryRepository.OnNewApplications(() => InvokeAsync(() =>
        {
            UpdateApplications();
            StateHasChanged();
        }));
        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        await this.InitializeViewModelAsync();
        TracesViewModel.ApplicationServiceId = ViewModel.SelectedApplication.Id;
        UpdateSubscription();
    }

    public MetricsPageState ConvertViewModelToSerializable()
    {
        return new MetricsPageState
        {
            ApplicationId = ViewModel.SelectedApplication.Id,
            MeterName = ViewModel.SelectedMeter?.MeterName,
            InstrumentName = ViewModel.SelectedInstrument?.Name,
            DurationMinutes = (int)ViewModel.SelectedDuration.Id.TotalMinutes
        };
    }

    public void UpdateViewModelFromQuery(MetricsViewModel viewModel)
    {
        viewModel.SelectedDuration = _durations.SingleOrDefault(d => (int)d.Id.TotalMinutes == DurationMinutes) ?? _durations.Single(d => d.Id == s_defaultDuration);
        viewModel.SelectedApplication = _applications.SingleOrDefault(e => e.Id == ApplicationInstanceId) ?? _selectApplication;
        viewModel.Instruments = !string.IsNullOrEmpty(viewModel.SelectedApplication.Id) ? TelemetryRepository.GetInstrumentsSummary(viewModel.SelectedApplication.Id) : null;

        viewModel.SelectedMeter = null;
        viewModel.SelectedInstrument = null;
        if (viewModel.Instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            viewModel.SelectedMeter = viewModel.Instruments.FirstOrDefault(i => i.Parent.MeterName == MeterName)?.Parent;
            if (viewModel.SelectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                viewModel.SelectedInstrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
                {
                    ApplicationServiceId = ApplicationInstanceId!,
                    MeterName = MeterName,
                    InstrumentName = InstrumentName
                });
            }
        }
    }

    private void UpdateApplications()
    {
        _applications = SelectViewModelFactory.CreateApplicationsSelectViewModel(TelemetryRepository.GetApplications());
        _applications.Insert(0, _selectApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        ViewModel.SelectedMeter = null;
        ViewModel.SelectedInstrument = null;
        return this.AfterViewModelChangedAsync();
    }

    private Task HandleSelectedDurationChangedAsync()
    {
        return this.AfterViewModelChangedAsync();
    }

    public sealed class MetricsViewModel
    {
        public FluentTreeItem? SelectedTreeItem { get; set; }
        public OtlpMeter? SelectedMeter { get; set; }
        public OtlpInstrument? SelectedInstrument { get; set; }
        public required SelectViewModel<string> SelectedApplication { get; set; }
        public SelectViewModel<TimeSpan> SelectedDuration { get; set; } = null!;
        public List<OtlpInstrument>? Instruments { get; set; }
    }

    public class MetricsPageState
    {
        public string? ApplicationId { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
        public int DurationMinutes { get; set; }
    }

    private Task HandleSelectedTreeItemChangedAsync()
    {
        if (ViewModel.SelectedTreeItem?.Data is OtlpMeter meter)
        {
            ViewModel.SelectedMeter = meter;
            ViewModel.SelectedInstrument = null;
        }
        else if (ViewModel.SelectedTreeItem?.Data is OtlpInstrument instrument)
        {
            ViewModel.SelectedMeter = instrument.Parent;
            ViewModel.SelectedInstrument = instrument;
        }
        else
        {
            ViewModel.SelectedMeter = null;
            ViewModel.SelectedInstrument = null;
        }

        return this.AfterViewModelChangedAsync();
    }

    public UrlState GetUrlFromSerializableViewModel(MetricsPageState serializable)
    {
        string path;
        if (serializable.ApplicationId is not null && serializable.MeterName is not null)
        {
            path = serializable.InstrumentName != null
                ? $"/{BasePath}/{serializable.ApplicationId}/Meter/{serializable.MeterName}/Instrument/{serializable.InstrumentName}"
                : $"/{BasePath}/{serializable.ApplicationId}/Meter/{serializable.MeterName}";
        }
        else if (serializable.ApplicationId != null)
        {
            path = $"/{BasePath}/{serializable.ApplicationId}";
        }
        else
        {
            path = $"/{BasePath}";
        }

        var queryParameters = new Dictionary<string, string?>();

        if (ViewModel.SelectedDuration.Id != s_defaultDuration)
        {
            queryParameters.Add("duration", serializable.DurationMinutes.ToString(CultureInfo.InvariantCulture));
        }

        return new UrlState(path, queryParameters);
    }

    private void UpdateSubscription()
    {
        var selectedApplication = (ViewModel.SelectedApplication ?? _selectApplication).Id;
        // Subscribe to updates.
        if (_metricsSubscription is null || _metricsSubscription.ApplicationId != selectedApplication)
        {
            _metricsSubscription?.Dispose();
            _metricsSubscription = TelemetryRepository.OnNewMetrics(selectedApplication, SubscriptionType.Read, async () =>
            {
                if (!string.IsNullOrEmpty(selectedApplication))
                {
                    // If there are more instruments than before then update the UI.
                    var instruments = TelemetryRepository.GetInstrumentsSummary(selectedApplication);

                    if (ViewModel.Instruments is null || instruments.Count > ViewModel.Instruments.Count)
                    {
                        ViewModel.Instruments = instruments;
                        await InvokeAsync(StateHasChanged);
                    }
                }
            });
        }
    }

    public void Dispose()
    {
        _applicationsSubscription?.Dispose();
        _metricsSubscription?.Dispose();
    }
}
