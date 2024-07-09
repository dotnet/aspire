// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable, IPageWithSessionAndUrlState<Metrics.MetricsViewModel, Metrics.MetricsPageState>
{
    private SelectViewModel<ResourceTypeDetails> _selectApplication = null!;
    private List<SelectViewModel<TimeSpan>> _durations = null!;
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);

    private List<SelectViewModel<ResourceTypeDetails>> _applications = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _metricsSubscription;

    public string BasePath => DashboardUrls.MetricsBasePath;
    public string SessionStorageKey => "Metrics_PageState";
    public MetricsViewModel PageViewModel { get; set; } = null!;

    [Parameter]
    public string? ApplicationName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "meter")]
    public string? MeterName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "instrument")]
    public string? InstrumentName { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "duration")]
    public int DurationMinutes { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewKindName { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required ProtectedSessionStorage SessionStorage { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel TracesViewModel { get; set; }

    [Inject]
    public required ILogger<Metrics> Logger { get; init; }

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
        };

        _selectApplication = new SelectViewModel<ResourceTypeDetails>
        {
            Id = null,
            Name = ControlsStringsLoc[ControlsStrings.SelectAResource]
        };

        PageViewModel = new MetricsViewModel
        {
            SelectedApplication = _selectApplication,
            SelectedDuration = _durations.Single(d => d.Id == s_defaultDuration),
            SelectedViewKind = null
        };

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
        TracesViewModel.ApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();
        UpdateSubscription();
    }

    public MetricsPageState ConvertViewModelToSerializable()
    {
        return new MetricsPageState
        {
            ApplicationName = PageViewModel.SelectedApplication.Id is not null ? PageViewModel.SelectedApplication.Name : null,
            MeterName = PageViewModel.SelectedMeter?.MeterName,
            InstrumentName = PageViewModel.SelectedInstrument?.Name,
            DurationMinutes = (int)PageViewModel.SelectedDuration.Id.TotalMinutes,
            ViewKind = PageViewModel.SelectedViewKind?.ToString()
        };
    }

    public void UpdateViewModelFromQuery(MetricsViewModel viewModel)
    {
        viewModel.SelectedDuration = _durations.SingleOrDefault(d => (int)d.Id.TotalMinutes == DurationMinutes) ?? _durations.Single(d => d.Id == s_defaultDuration);
        viewModel.SelectedApplication = _applications.GetApplication(Logger, ApplicationName, _selectApplication);
        var selectedInstance = viewModel.SelectedApplication.Id?.GetApplicationKey();
        viewModel.Instruments = selectedInstance != null ? TelemetryRepository.GetInstrumentsSummary(selectedInstance.Value) : null;

        viewModel.SelectedMeter = null;
        viewModel.SelectedInstrument = null;
        viewModel.SelectedViewKind = Enum.TryParse(typeof(MetricViewKind), ViewKindName, out var view) && view is MetricViewKind vk ? vk : null;

        if (viewModel.Instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            viewModel.SelectedMeter = viewModel.Instruments.FirstOrDefault(i => i.Parent.MeterName == MeterName)?.Parent;
            if (viewModel.SelectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                viewModel.SelectedInstrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
                {
                    ApplicationKey = selectedInstance!.Value,
                    MeterName = MeterName,
                    InstrumentName = InstrumentName
                });
            }
        }
    }

    private void UpdateApplications()
    {
        _applications = ApplicationsSelectHelpers.CreateApplications(TelemetryRepository.GetApplications());
        _applications.Insert(0, _selectApplication);
        UpdateSubscription();
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        PageViewModel.SelectedMeter = null;
        PageViewModel.SelectedInstrument = null;
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
        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
        public SelectViewModel<TimeSpan> SelectedDuration { get; set; } = null!;
        public List<OtlpInstrument>? Instruments { get; set; }
        public required MetricViewKind? SelectedViewKind { get; set; }
    }

    public class MetricsPageState
    {
        public string? ApplicationName { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
        public int DurationMinutes { get; set; }
        public required string? ViewKind { get; set; }
    }

    public enum MetricViewKind
    {
        Table,
        Graph
    }

    private Task HandleSelectedTreeItemChangedAsync()
    {
        if (PageViewModel.SelectedTreeItem?.Data is OtlpMeter meter)
        {
            PageViewModel.SelectedMeter = meter;
            PageViewModel.SelectedInstrument = null;
        }
        else if (PageViewModel.SelectedTreeItem?.Data is OtlpInstrument instrument)
        {
            PageViewModel.SelectedMeter = instrument.Parent;
            PageViewModel.SelectedInstrument = instrument;
        }
        else
        {
            PageViewModel.SelectedMeter = null;
            PageViewModel.SelectedInstrument = null;
        }

        return this.AfterViewModelChangedAsync();
    }

    public string GetUrlFromSerializableViewModel(MetricsPageState serializable)
    {
        var duration = PageViewModel.SelectedDuration.Id != s_defaultDuration
            ? (int?)serializable.DurationMinutes
            : null;

        var url = DashboardUrls.MetricsUrl(
            resource: serializable.ApplicationName,
            meter: serializable.MeterName,
            instrument: serializable.InstrumentName,
            duration: duration,
            view: serializable.ViewKind);

        return url;
    }

    private async Task OnViewChangedAsync(MetricViewKind newView)
    {
        PageViewModel.SelectedViewKind = newView;
        await this.AfterViewModelChangedAsync();
    }

    private void UpdateSubscription()
    {
        var selectedApplicationKey = PageViewModel.SelectedApplication.Id?.GetApplicationKey();

        // Subscribe to updates.
        if (_metricsSubscription is null || _metricsSubscription.ApplicationKey != selectedApplicationKey)
        {
            _metricsSubscription?.Dispose();
            _metricsSubscription = TelemetryRepository.OnNewMetrics(selectedApplicationKey, SubscriptionType.Read, async () =>
            {
                if (selectedApplicationKey != null)
                {
                    // If there are more instruments than before then update the UI.
                    var instruments = TelemetryRepository.GetInstrumentsSummary(selectedApplicationKey.Value);

                    if (PageViewModel.Instruments is null || instruments.Count > PageViewModel.Instruments.Count)
                    {
                        PageViewModel.Instruments = instruments;
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
