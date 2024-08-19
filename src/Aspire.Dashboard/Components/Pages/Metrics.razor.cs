// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Metrics : IDisposable, IPageWithSessionAndUrlState<Metrics.MetricsViewModel, Metrics.MetricsPageState>
{
    private SelectViewModel<ResourceTypeDetails> _selectApplication = null!;
    private List<SelectViewModel<TimeSpan>> _durations = null!;
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);
    private AspirePageContentLayout? _contentLayout;

    private List<OtlpApplication> _applications = default!;
    private List<SelectViewModel<ResourceTypeDetails>> _applicationViewModels = default!;
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
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required ISessionStorage SessionStorage { get; init; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    [Inject]
    public required IDashpagePersistence DashpagePersistence { get; init; }

    [Inject]
    public required ILogger<Metrics> Logger { get; init; }

    protected override async Task OnInitializedAsync()
    {
        _durations =
        [
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastOneMinute)], Id = TimeSpan.FromMinutes(1) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastFiveMinutes)], Id = TimeSpan.FromMinutes(5) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastFifteenMinutes)], Id = TimeSpan.FromMinutes(15) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastThirtyMinutes)], Id = TimeSpan.FromMinutes(30) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastHour)], Id = TimeSpan.FromHours(1) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastThreeHours)], Id = TimeSpan.FromHours(3) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastSixHours)], Id = TimeSpan.FromHours(6) },
            new() { Name = Loc[nameof(Dashboard.Resources.Metrics.MetricsLastTwelveHours)], Id = TimeSpan.FromHours(12) },
        ];

        _selectApplication = new SelectViewModel<ResourceTypeDetails>
        {
            Id = null,
            Name = ControlsStringsLoc[ControlsStrings.None]
        };

        PageViewModel = new MetricsViewModel
        {
            SelectedApplication = _selectApplication,
            SelectedDuration = _durations.Single(d => d.Id == s_defaultDuration),
            SelectedViewKind = null,
            Dashpages = await DashpagePersistence.GetDashpagesAsync(CancellationToken.None)
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
        await this.InitializeViewModelAsync();
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
        viewModel.SelectedApplication = _applicationViewModels.GetApplication(Logger, ApplicationName, _selectApplication);
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
        _applications = TelemetryRepository.GetApplications();
        _applicationViewModels = ApplicationsSelectHelpers.CreateApplications(_applications);
        _applicationViewModels.Insert(0, _selectApplication);

        PageViewModel.ApplicationNames = _applications.Select(a => a.ApplicationName).ToImmutableHashSet(StringComparers.OtlpApplicationName);

        UpdateSubscription();
    }

    private Task HandleDashpageMetricViewKindChangedAsync(MenuChangeEventArgs e)
    {
        Debug.Assert(e.Id is not null);
        var metricKind = Enum.Parse<MetricViewKind>(e.Id);
        PageViewModel.SelectedViewKind = metricKind;
        return this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
    }

    private Task HandleSelectedApplicationChangedAsync()
    {
        // The new resource might not have the currently selected meter/instrument.
        // Check whether the new resource has the current values or not, and clear if they're not available.
        if (PageViewModel.SelectedMeter != null ||
            PageViewModel.SelectedInstrument != null)
        {
            var selectedInstance = PageViewModel.SelectedApplication.Id?.GetApplicationKey();
            var instruments = selectedInstance != null ? TelemetryRepository.GetInstrumentsSummary(selectedInstance.Value) : null;

            if (instruments == null || ShouldClearSelectedMetrics(instruments))
            {
                PageViewModel.SelectedMeter = null;
                PageViewModel.SelectedInstrument = null;
            }
        }

        return this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
    }

    private bool ShouldClearSelectedMetrics(List<OtlpInstrument> instruments)
    {
        if (PageViewModel.SelectedMeter != null && !instruments.Any(i => i.Parent.MeterName == PageViewModel.SelectedMeter.MeterName))
        {
            return true;
        }
        if (PageViewModel.SelectedInstrument != null && !instruments.Any(i => i.Name == PageViewModel.SelectedInstrument.Name))
        {
            return true;
        }

        return false;
    }

    private Task HandleSelectedDurationChangedAsync()
    {
        return this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: true);
    }

    #region Model

    public sealed class MetricsViewModel
    {
        public FluentTreeItem? SelectedTreeItem { get; set; }
        public OtlpMeter? SelectedMeter { get; set; }
        public OtlpInstrument? SelectedInstrument { get; set; }
        public DashpageDefinition? SelectedDashpage { get; set; }

        public required SelectViewModel<ResourceTypeDetails> SelectedApplication { get; set; }
        public required SelectViewModel<TimeSpan> SelectedDuration { get; set; }

        /// <summary>
        /// The set of instruments applicable to the selected application.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> when no application is selected.
        /// </remarks>
        public List<OtlpInstrument>? Instruments { get; set; }

        public required MetricViewKind? SelectedViewKind { get; set; }

        public required ImmutableArray<DashpageDefinition> Dashpages { get; init; }

        public ImmutableHashSet<string> ApplicationNames { get; set; } = [];

        internal bool IsDashpageAvailable(DashpageDefinition dashpage)
        {
            if (dashpage.Charts.Count == 0)
            {
                return false;
            }

            bool foundOne = false;
            bool allExplicitResources = true;

            foreach (var chart in dashpage.Charts)
            {
                bool isInstrumentAvailable = IsInstrumentAvailable(chart.InstrumentName);

                if (isInstrumentAvailable)
                {
                    foundOne = true;
                }
                else if (chart.IsRequired)
                {
                    // A required chart's instrument isn't available.
                    return false;
                }

                if (chart.ResourceName is not null)
                {
                    if (IsResourceAvailable(chart.ResourceName))
                    {
                        foundOne = true;
                    }
                    else
                    {
                        // A chart's explicit resource isn't available.
                        return false;
                    }
                }
                else
                {
                    allExplicitResources = false;
                }
            }

            if (!allExplicitResources && SelectedApplication.Id is null)
            {
                // No resource is selected and at least one chart doesn't specify an explicit resource.
                return false;
            }

            return foundOne;

            bool IsInstrumentAvailable(string instrumentName)
            {
                return Instruments?.Any(i => string.Equals(i.Name, instrumentName, StringComparisons.OtlpInstrumentName)) ?? false;
            }

            bool IsResourceAvailable(string resourceName)
            {
                return ApplicationNames.Contains(resourceName);
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MetricViewKind
    {
        [EnumMember(Value = "graph")]
        Graph,
        [EnumMember(Value = "table")]
        Table
    }

    public class MetricsPageState
    {
        public string? ApplicationName { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
        public int DurationMinutes { get; set; }
        public required string? ViewKind { get; set; }
    }

    #endregion

    private Task HandleSelectedTreeItemChangedAsync()
    {
        (OtlpMeter?, OtlpInstrument?, DashpageDefinition?) selections =
            PageViewModel.SelectedTreeItem?.Data switch
            {
                OtlpMeter meter => (meter, null, null),
                OtlpInstrument instrument => (instrument.Parent, instrument, null),
                DashpageDefinition dashpage => (null, null, dashpage),
                _ => (null, null, null)
            };

        var vm = PageViewModel;

        (vm.SelectedMeter, vm.SelectedInstrument, vm.SelectedDashpage) = selections;

        return this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: false);
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
        await this.AfterViewModelChangedAsync(_contentLayout, isChangeInToolbar: false);
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
