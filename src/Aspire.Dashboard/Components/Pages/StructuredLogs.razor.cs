// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class StructuredLogs : IAsyncDisposable
{
    private TotalItemsFooter _totalItemsFooter = default!;
    private Subscription? _logsSubscription;
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
            _selectedProject = value;
            ViewModel.ApplicationServiceId = _selectedProject?.Uid;
            UpdateSubscription();
        }
    }

    [Parameter]
    public string? ApplicationName { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    [Inject]
    public required StructuredLogsViewModel ViewModel { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; set; }

    [Inject]
    public required IJSRuntime JS { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? TraceId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? SpanId { get; set; }

    private async ValueTask<GridItemsProviderResult<OtlpLogEntry>> GetData(GridItemsProviderRequest<OtlpLogEntry> request)
    {
        await _resourceSelectorViewModel.LoadedAsync;

        ViewModel.StartIndex = request.StartIndex;
        ViewModel.Count = request.Count;

        var logs = ViewModel.GetLogs();

        // Updating the total item count as a field doesn't work because it isn't updated with the grid.
        // The workaround is to put the count inside a control and explicitly update and refresh the control.
        _totalItemsFooter.SetTotalItemCount(logs.TotalItemCount);

        return GridItemsProviderResult.From(logs.Items, logs.TotalItemCount);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(TraceId))
        {
            ViewModel.AddFilter(new LogFilter { Field = "TraceId", Condition = FilterCondition.Equals, Value = TraceId });
        }
        if (!string.IsNullOrEmpty(SpanId))
        {
            ViewModel.AddFilter(new LogFilter { Field = "SpanId", Condition = FilterCondition.Equals, Value = SpanId });
        }

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

    public Task HandleSelectedApplicationChangedAsync(ProjectViewModel? project)
    {
        SelectedProject = project;
        NavigationManager.NavigateTo($"/StructuredLogs/{project?.Name}");
        _applicationChanged = true;

        return Task.CompletedTask;
    }

    private void UpdateSubscription()
    {
        // Subscribe to updates.
        if (_logsSubscription is null || _logsSubscription.ApplicationId != SelectedProject?.Uid)
        {
            _logsSubscription?.Dispose();
            _logsSubscription = TelemetryRepository.OnNewLogs(SelectedProject?.Uid, async () =>
            {
                ViewModel.ClearData();
                await InvokeAsync(StateHasChanged);
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

    private async Task OnShowProperties(OtlpLogEntry entry)
    {
        var entryProperties = entry.AllProperties()
            .Select(kvp => new LogEntryPropertyViewModel { Name = kvp.Key, Value = kvp.Value })
            .ToList();

        var parameters = new DialogParameters
        {
            Title = "Log Entry Details",
            Width = "auto",
            Height = "auto",
            TrapFocus = true,
            Modal = true,
            PrimaryAction = "Close",
            PrimaryActionEnabled = true,
            SecondaryAction = null,
        };
        await DialogService.ShowDialogAsync<LogDetailsDialog>(entryProperties, parameters);
    }

    private async Task OpenFilterAsync(LogFilter? entry)
    {
        var logPropertyKeys = TelemetryRepository.GetLogPropertyKeys(SelectedProject?.Uid);

        var title = entry is not null ? "Edit Filter" : "Add Filter";
        var parameters = new DialogParameters
        {
            OnDialogResult = DialogService.CreateDialogCallback(this, HandleFilterDialog),
            Title = title,
            Alignment = HorizontalAlignment.Right,
            PrimaryAction = null,
            SecondaryAction = null,
        };
        var data = new FilterDialogViewModel
        {
            Filter = entry,
            LogPropertyKeys = logPropertyKeys
        };
        await DialogService.ShowPanelAsync<FilterDialog>(data, parameters);
    }

    private Task HandleFilterDialog(DialogResult result)
    {
        if (result.Data is FilterDialogResult filterResult && filterResult.Filter is LogFilter filter)
        {
            if (filterResult.Delete)
            {
                ViewModel.RemoveFilter(filter);
            }
            else if (filterResult.Add)
            {
                ViewModel.AddFilter(filter);
            }
        }

        return Task.CompletedTask;
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
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initializeContinuousScroll");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logsSubscription?.Dispose();
        _filterCts?.Dispose();
        await _resourceSelectorViewModel.DisposeAsync();
    }
}
