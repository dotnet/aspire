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

public partial class Metrics : IAsyncDisposable
{
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
    private static readonly TimeSpan s_defaultDuration = TimeSpan.FromMinutes(5);

    private MetricsDurationViewModel _selectedDuration = s_durations.Single(d => d.Duration == s_defaultDuration);
    private Subscription? _metricsSubscription;
    private List<OtlpInstrument>? _instruments;
    private FluentTreeItem? _selectedTreeItem;
    private OtlpMeter? _selectedMeter;
    private OtlpInstrument? _selectedInstrument;
    private ProjectViewModel? _selectedProject;
    private ResourceSelectorViewModel<ProjectViewModel> _resourceSelectorViewModel = default!;

    private ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (_selectedProject != value)
            {
                _selectedProject = value;
                ViewModel.ApplicationServiceId = _selectedProject?.Uid;
                UpdateSubscription();
            }
        }
    }

    [Parameter]
    public string? ApplicationName { get; set; }

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
    public required IDashboardViewModelService DashboardViewModelService { get; set; }

    [Inject]
    public required ProtectedSessionStorage ProtectedSessionStore { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel ViewModel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _resourceSelectorViewModel = new ResourceSelectorViewModel<ProjectViewModel>()
        {
            ResourceGetter = DashboardViewModelService.GetProjectsAsync,
            ResourceWatcher = ct => DashboardViewModelService.WatchProjectsAsync(cancellationToken: ct),
            ResourcesLoaded = ProjectsLoaded,
            SelectedResourceChanged = HandleSelectedApplicationChangedAsync,
            UnselectedText = "Select service..."
        };
        await _resourceSelectorViewModel.InitializeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _selectedDuration = s_durations.SingleOrDefault(d => (int)d.Duration.TotalMinutes == DurationMinutes) ?? s_durations.Single(d => d.Duration == s_defaultDuration);
		await _resourceSelectorViewModel.LoadedAsync;

        _instruments = !string.IsNullOrEmpty(SelectedProject?.Uid) ? TelemetryRepository.GetInstrumentsSummary(SelectedProject.Uid) : null;

        _selectedMeter = null;
        _selectedInstrument = null;
        if (_instruments != null && !string.IsNullOrEmpty(MeterName))
        {
            _selectedMeter = _instruments.FirstOrDefault(i => i.Parent.MeterName == MeterName)?.Parent;
            if (_selectedMeter != null && !string.IsNullOrEmpty(InstrumentName))
            {
                _selectedInstrument = TelemetryRepository.GetInstrument(new GetInstrumentRequest
                {
                    ApplicationServiceId = SelectedProject!.Uid,
                    MeterName = MeterName,
                    InstrumentName = InstrumentName
                });
            }
        }

        UpdateSubscription();
    }

    public async Task HandleSelectedApplicationChangedAsync(ProjectViewModel? project)
    {
        SelectedProject = project;
        var state = new MetricsSelectedState { ApplicationName = SelectedProject?.Name };

        NavigateTo(state);
        await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
    }

    private async Task HandleSelectedDurationChangedAsync()
    {
        var state = new MetricsSelectedState { ApplicationName = SelectedProject?.Name, DurationMinutes = (int)_selectedDuration.Duration.TotalMinutes, InstrumentName = InstrumentName, MeterName = MeterName };

        NavigateTo(state);
        await ProtectedSessionStore.SetAsync(MetricsSelectedState.Key, state);
    }

    private sealed class MetricsSelectedState
    {
        public const string Key = "Metrics_SelectState";
        public string? ApplicationName { get; set; }
        public string? MeterName { get; set; }
        public string? InstrumentName { get; set; }
        public int DurationMinutes { get; set; }
    }

    private async Task HandleSelectedTreeItemChanged()
    {
        MetricsSelectedState state;

        if (_selectedTreeItem?.Data is OtlpMeter meter)
        {
            state = new MetricsSelectedState { ApplicationName = SelectedProject?.Name, DurationMinutes = (int)_selectedDuration.Duration.TotalMinutes, MeterName = meter.MeterName };
        }
        else if (_selectedTreeItem?.Data is OtlpInstrument instrument)
        {
            state = new MetricsSelectedState { ApplicationName = SelectedProject?.Name, DurationMinutes = (int)_selectedDuration.Duration.TotalMinutes, MeterName = instrument.Parent.MeterName, InstrumentName = instrument.Name };
        }
        else
        {
            state = new MetricsSelectedState { ApplicationName = SelectedProject?.Name, DurationMinutes = (int)_selectedDuration.Duration.TotalMinutes };
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
                url = $"/Metrics/{state.ApplicationName}/Meter/{state.MeterName}/Instrument/{state.InstrumentName}";
            }
            else
            {
                url = $"/Metrics/{state.ApplicationName}/Meter/{state.MeterName}";
            }
        }
        else if (state.ApplicationName != null)
        {
            url = $"/Metrics/{state.ApplicationName}";
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
        if (_metricsSubscription is null || _metricsSubscription.ApplicationId != SelectedProject?.Uid)
        {
            _metricsSubscription?.Dispose();
            _metricsSubscription = TelemetryRepository.OnNewMetrics(SelectedProject?.Uid, async () =>
            {
                var selectedApplicationId = SelectedProject?.Uid;
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

    public async Task ProjectsLoaded(ResourcesLoadedEventArgs<ProjectViewModel> args)
    {
        if (!string.IsNullOrEmpty(ApplicationName))
        {
            args.SelectedItem = args.Resources.SingleOrDefault(r => r.Resource?.Name == ApplicationName);
            SelectedProject = args.SelectedItem?.Resource;
            await InvokeAsync(StateHasChanged);
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

    public async ValueTask DisposeAsync()
    {
        _metricsSubscription?.Dispose();
        await _resourceSelectorViewModel.DisposeAsync();
    }
}
