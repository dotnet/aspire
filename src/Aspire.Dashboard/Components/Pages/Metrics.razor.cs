// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable, IComponentWithTelemetry, IPageWithSessionAndUrlState<Metrics.MetricsViewModel, Metrics.MetricsPageState>
{
    private SelectViewModel<ResourceTypeDetails> _selectApplication = null!;
    private List<SelectViewModel<TimeSpan>> _durations = null!;
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);
    private AspirePageContentLayout? _contentLayout;
    private TreeMetricSelector? _treeMetricSelector;

    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
    private Subscription? _applicationsSubscription;
    private Subscription? _metricsSubscription;

    public string BasePath => DashboardUrls.MetricsBasePath;
    public string SessionStorageKey => BrowserStorageKeys.MetricsPageState;
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
    public int? DurationMinutes { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewKindName { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ISessionStorage SessionStorage { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required ILogger<Metrics> Logger { get; init; }

    [Inject]
    public required ComponentTelemetryContextProvider TelemetryContextProvider { get; init; }

    [Inject]
    public required PauseManager PauseManager { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    protected override void OnInitialized()
    {
        TelemetryContextProvider.Initialize(TelemetryContext);

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
            Name = ControlsStringsLoc[nameof(ControlsStrings.LabelNone)]
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
    }

    protected override async Task OnParametersSetAsync()
    {
        if (await this.InitializeViewModelAsync())
        {
            return;
        }

        UpdateSubscription();
        UpdateTelemetryProperties();
    }

    public MetricsPageState ConvertViewModelToSerializable()
    {
        return new MetricsPageState
        {
            ApplicationName = PageViewModel.SelectedApplication.Id is not null ? PageViewModel.SelectedApplication.Name : null,
            MeterName = PageViewModel.SelectedMeter?.Name,
            InstrumentName = PageViewModel.SelectedInstrument?.Name,
            DurationMinutes = (int)PageViewModel.SelectedDuration.Id.TotalMinutes,
            ViewKind = PageViewModel.SelectedViewKind?.ToString()
        };
    }

    public Task UpdateViewModelFromQueryAsync(MetricsViewModel viewModel)
    {
        if (ApplicationName is null && TryGetSingleResource() is { } r)
        {
            // If there is no app selected and there is only one application available, select it.
            PageViewModel.SelectedApplication = r;
            ApplicationName = r.Name;
            return this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
        }

        viewModel.SelectedDuration = _durations.SingleOrDefault(d => (int)d.Id.TotalMinutes == DurationMinutes) ?? _durations.Single(d => d.Id == s_defaultDuration);
        viewModel.SelectedApplication = _applicationViewModels.GetApplication(Logger, ApplicationName, canSelectGrouping: true, _selectApplication);

        UpdateInstruments(viewModel);

        viewModel.SelectedMeter = null;
        viewModel.SelectedInstrument = null;
        viewModel.SelectedViewKind = Enum.TryParse(typeof(MetricViewKind), ViewKindName, out var view) && view is MetricViewKind vk ? vk : null;

        if (viewModel.Instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            viewModel.SelectedMeter = viewModel.Instruments.FirstOrDefault(i => i.Parent.Name == MeterName)?.Parent;
            if (viewModel.SelectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                viewModel.SelectedInstrument = viewModel.Instruments.FirstOrDefault(i => i.Parent.Name == MeterName && i.Name == InstrumentName);
            }
        }
        return Task.CompletedTask;

        SelectViewModel<ResourceTypeDetails>? TryGetSingleResource()
        {
            var apps = _applicationViewModels.Where(e => e != _selectApplication).ToList();
            return apps.Count == 1 ? apps[0] : null;
        }
    }

    private void UpdateInstruments(MetricsViewModel viewModel)
    {
        var selectedInstance = viewModel.SelectedApplication.Id?.GetApplicationKey();
        viewModel.Instruments = selectedInstance != null ? TelemetryRepository.GetInstrumentsSummaries(selectedInstance.Value) : null;
    }

    private void UpdateApplications()
    {
        _applications = TelemetryRepository.GetApplications();
        _applicationViewModels = ApplicationsSelectHelpers.CreateApplications(_applications);
        _applicationViewModels.Insert(0, _selectApplication);
        UpdateSubscription();
    }

    private async Task HandleSelectedApplicationChangedAsync()
    {
        UpdateInstruments(PageViewModel);

        // The new resource might not have the currently selected meter/instrument.
        // Check whether the new resource has the current values or not, and clear if they're not available.
        if (PageViewModel.SelectedMeter != null ||
            PageViewModel.SelectedInstrument != null)
        {
            if (PageViewModel.Instruments == null || ShouldClearSelectedMetrics(PageViewModel.Instruments))
            {
                PageViewModel.SelectedMeter = null;
                PageViewModel.SelectedInstrument = null;
            }
        }

        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: true);

        // The mobile view doesn't update the URL when the application changes.
        // Because of this, the page doesn't autoamtically use updated instruments.
        // Force the metrics tree to update so it re-renders with the new app's instruments.
        _treeMetricSelector?.OnResourceChanged();
    }

    private bool ShouldClearSelectedMetrics(List<OtlpInstrumentSummary> instruments)
    {
        if (PageViewModel.SelectedMeter != null && !instruments.Any(i => i.Parent.Name == PageViewModel.SelectedMeter.Name))
        {
            return true;
        }
        if (PageViewModel.SelectedInstrument != null && !instruments.Any(i => i.Name == PageViewModel.SelectedInstrument.Name))
        {
            return true;
        }

        return false;
    }

    private Task ClearMetrics(ApplicationKey? key)
    {
        TelemetryRepository.ClearMetrics(key);
        return Task.CompletedTask;
    }

    private Task HandleSelectedDurationChangedAsync()
    {
        return this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: true);
    }

    private string? PauseText => PauseManager.AreMetricsPaused(out var startTime)
        ? string.Format(
            CultureInfo.CurrentCulture,
            Loc[nameof(Dashboard.Resources.Metrics.PauseInProgressText)],
            FormatHelpers.FormatTimeWithOptionalDate(TimeProvider, startTime.Value, MillisecondsDisplay.Truncated))
        : null;

    public sealed class MetricsViewModel
    {
        public FluentTreeItem? SelectedTreeItem { get; set; }
        public OtlpScope? SelectedMeter { get; set; }
        public OtlpInstrumentSummary? SelectedInstrument { get; set; }
        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
        public SelectViewModel<TimeSpan> SelectedDuration { get; set; } = null!;
        public List<OtlpInstrumentSummary>? Instruments { get; set; }
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
        if (PageViewModel.SelectedTreeItem?.Data is OtlpScope meter)
        {
            PageViewModel.SelectedMeter = meter;
            PageViewModel.SelectedInstrument = null;
        }
        else if (PageViewModel.SelectedTreeItem?.Data is OtlpInstrumentSummary instrument)
        {
            PageViewModel.SelectedMeter = instrument.Parent;
            PageViewModel.SelectedInstrument = instrument;
        }
        else
        {
            PageViewModel.SelectedMeter = null;
            PageViewModel.SelectedInstrument = null;
        }

        return this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
    }

    public string GetUrlFromSerializableViewModel(MetricsPageState serializable)
    {
        var url = DashboardUrls.MetricsUrl(
            resource: serializable.ApplicationName,
            meter: serializable.MeterName,
            instrument: serializable.InstrumentName,
            duration: serializable.DurationMinutes,
            view: serializable.ViewKind);

        return url;
    }

    private async Task OnViewChangedAsync(MetricViewKind newView)
    {
        PageViewModel.SelectedViewKind = newView;
        await this.AfterViewModelChangedAsync(_contentLayout, waitToApplyMobileChange: false);
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
                    var instruments = TelemetryRepository.GetInstrumentsSummaries(selectedApplicationKey.Value);

                    if (PageViewModel.Instruments is null || instruments.Count != PageViewModel.Instruments.Count)
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
        TelemetryContext.Dispose();
    }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new(DashboardUrls.MetricsBasePath);

    public void UpdateTelemetryProperties()
    {
        TelemetryContext.UpdateTelemetryProperties([
            new ComponentTelemetryProperty(TelemetryPropertyKeys.MetricsApplicationIsReplica, new AspireTelemetryProperty(PageViewModel.SelectedApplication.Id?.ReplicaSetName is not null)),
            new ComponentTelemetryProperty(TelemetryPropertyKeys.MetricsInstrumentsCount, new AspireTelemetryProperty((PageViewModel.Instruments?.Count ?? -1).ToString(CultureInfo.InvariantCulture), AspireTelemetryPropertyType.Metric)),
            new ComponentTelemetryProperty(TelemetryPropertyKeys.MetricsSelectedDuration, new AspireTelemetryProperty(PageViewModel.SelectedDuration.Id.ToString(), AspireTelemetryPropertyType.UserSetting)),
            new ComponentTelemetryProperty(TelemetryPropertyKeys.MetricsSelectedView, new AspireTelemetryProperty(PageViewModel.SelectedViewKind?.ToString() ?? string.Empty, AspireTelemetryPropertyType.UserSetting))
        ], Logger);
    }
}
