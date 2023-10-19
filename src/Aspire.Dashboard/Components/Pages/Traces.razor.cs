// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Traces : IAsyncDisposable
{
    private TotalItemsFooter _totalItemsFooter = default!;
    private Subscription? _tracesSubscription;
    private bool _applicationChanged;
    private CancellationTokenSource? _filterCts;
    private string _filter = string.Empty;
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

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required TracesViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; set; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    private string GetRowStyle(OtlpTrace trace)
    {
        var percentage = 0.0;
        if (ViewModel.MaxDuration != TimeSpan.Zero)
        {
            percentage = trace.Duration / ViewModel.MaxDuration * 100.0;
        }

        return $"background: linear-gradient(to right, var(--neutral-fill-input-alt-active) {percentage:0.##}%, transparent {percentage:0.##}%);";
    }

    private ValueTask<GridItemsProviderResult<OtlpTrace>> GetData(GridItemsProviderRequest<OtlpTrace> request)
    {
        ViewModel.StartIndex = request.StartIndex;
        ViewModel.Count = request.Count;

        var traces = ViewModel.GetTraces();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(traces.TotalItemCount);

        return ValueTask.FromResult(GridItemsProviderResult.From(traces.Items, traces.TotalItemCount));
    }

    protected override async Task OnInitializedAsync()
    {
        _resourceSelectorViewModel = new ResourceSelectorViewModel<ProjectViewModel>()
        {
            ResourceGetter = DashboardViewModelService.GetProjectsAsync,
            ResourceWatcher = ct => DashboardViewModelService.WatchProjectsAsync(cancellationToken: ct),
            ResourcesLoaded = ProjectsLoaded,
            SelectedResourceChanged = HandleSelectedApplicationChangedAsync,
            UnselectedText = "(All)"
        };
        await _resourceSelectorViewModel.InitializeAsync();
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

    public Task HandleSelectedApplicationChangedAsync(ProjectViewModel? project)
    {
        SelectedProject = project;
        NavigationManager.NavigateTo($"/Traces/{project?.Name}");
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_tracesSubscription is null || _tracesSubscription.ApplicationId != SelectedProject?.Uid)
        {
            _tracesSubscription?.Dispose();
            _tracesSubscription = TelemetryRepository.OnNewTraces(SelectedProject?.Uid, async () =>
            {
                ViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            _filter = newFilter;
            _filterCts?.Cancel();

            // Debouncing logic. Apply the filter after a delay.
            var cts = _filterCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                await Task.Delay(400, cts.Token);
                ViewModel.FilterText = newFilter ?? string.Empty;
                await InvokeAsync(StateHasChanged);
            });
        }
    }

    private void HandleClear(string value)
    {
        _filterCts?.Cancel();
        ViewModel.FilterText = string.Empty;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_applicationChanged)
        {
            await JS.InvokeVoidAsync("resetContinuousScrollPosition");
            _applicationChanged = false;
        }
        await JS.InvokeVoidAsync("initializeContinuousScroll");
    }

    public async ValueTask DisposeAsync()
    {
        _tracesSubscription?.Dispose();
        await _resourceSelectorViewModel.DisposeAsync();
    }
}
