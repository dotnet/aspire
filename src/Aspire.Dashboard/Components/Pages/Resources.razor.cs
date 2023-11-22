// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IDisposable
{
    private Subscription? _logsSubscription;
    private Dictionary<OtlpApplication, int>? _applicationUnviewedErrorCounts;

    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; init; }
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    [Inject]
    public required IJSRuntime JS { get; set; }

    private IEnumerable<EnvironmentVariableViewModel>? SelectedEnvironmentVariables { get; set; }
    private string? SelectedResourceName { get; set; }

    private static ViewModelMonitor<ResourceViewModel> GetViewModelMonitor(IDashboardViewModelService dashboardViewModelService)
        => dashboardViewModelService.GetResources();

    private bool Filter(ResourceViewModel resource)
        => ((resource.ResourceType == "Project" && _areProjectsVisible) ||
            (resource.ResourceType == "Container" && _areContainersVisible) ||
            (resource.ResourceType == "Executable" && _areExecutablesVisible)) &&
           (resource.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            (resource is ContainerViewModel containerViewModel &&
             containerViewModel.Image.Contains(_filter, StringComparison.CurrentCultureIgnoreCase)));

    private readonly Dictionary<string, ResourceViewModel> _resourcesMap = new();
    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private string _filter = "";
    private bool _isTypeFilterVisible;
    private bool _allTypesVisibleCheckboxValue = true;
    private bool _areProjectsVisible = true;
    private bool _areContainersVisible = true;
    private bool _areExecutablesVisible = true;
    private FluentCheckbox? _allTypesVisibleCheckbox;

    private bool AreAllTypesVisible => _areProjectsVisible && _areContainersVisible && _areExecutablesVisible;

    private void HandleTypeFilterShowAllChanged(bool newValue)
    {
        _allTypesVisibleCheckboxValue = _areProjectsVisible = _areContainersVisible = _areExecutablesVisible = newValue;
    }

    private async Task HandleTypeFilterTypeChanged()
    {
        if (_areProjectsVisible && _areContainersVisible && _areExecutablesVisible)
        {
            _allTypesVisibleCheckboxValue = true;
            await SetIndeterminateState(false);
        }
        else if (!_areProjectsVisible && !_areContainersVisible && !_areExecutablesVisible)
        {
            _allTypesVisibleCheckboxValue = false;
            await SetIndeterminateState(false);
        }
        else
        {
            _allTypesVisibleCheckboxValue = true; // Set this to true so the styling is consistent while indeterminate
            await SetIndeterminateState(true);
        }
    }

    private async Task SetIndeterminateState(bool indeterminate)
    {
        if (_allTypesVisibleCheckbox is not null)
        {
            await JS.InvokeVoidAsync("setIndeterminate", _allTypesVisibleCheckbox.Element, indeterminate);
        }
    }

    private IQueryable<ResourceViewModel>? FilteredResources => _resourcesMap.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);

    protected override void OnInitialized()
    {
        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();
        var viewModelMonitor = GetViewModelMonitor(DashboardViewModelService);
        var resources = viewModelMonitor.Snapshot;
        var watch = viewModelMonitor.Watch;
        foreach (var resource in resources)
        {
            _resourcesMap.Add(resource.Name, resource);
        }

        _ = Task.Run(async () =>
        {
            await foreach (var resourceChanged in watch.WithCancellation(_watchTaskCancellationTokenSource.Token))
            {
                await OnResourceListChanged(resourceChanged.ObjectChangeType, resourceChanged.Resource);
            }
        });

        _logsSubscription = TelemetryRepository.OnNewLogs(null, SubscriptionType.Other, async () =>
        {
            _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();
            await InvokeAsync(StateHasChanged);
        });
    }

    private int GetUnviewedErrorCount(ResourceViewModel resource)
    {
        if (_applicationUnviewedErrorCounts is null)
        {
            return 0;
        }

        var application = TelemetryRepository.GetApplication(resource.Uid);
        if (application is null)
        {
            return 0;
        }

        if (!_applicationUnviewedErrorCounts.TryGetValue(application, out var count))
        {
            return 0;
        }

        return count;
    }

    private void ShowEnvironmentVariables(ResourceViewModel resource)
    {
        if (SelectedEnvironmentVariables == resource.Environment)
        {
            ClearSelectedResource();
        }
        else
        {
            SelectedEnvironmentVariables = resource.Environment;
            SelectedResourceName = resource.Name;
        }
    }

    private void ClearSelectedResource()
    {
        SelectedEnvironmentVariables = null;
        SelectedResourceName = null;
    }

    private async Task OnResourceListChanged(ObjectChangeType objectChangeType, ResourceViewModel resource)
    {
        switch (objectChangeType)
        {
            case ObjectChangeType.Added:
                _resourcesMap.Add(resource.Name, resource);
                break;

            case ObjectChangeType.Modified:
                _resourcesMap[resource.Name] = resource;
                break;

            case ObjectChangeType.Deleted:
                _resourcesMap.Remove(resource.Name);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watchTaskCancellationTokenSource.Cancel();
            _watchTaskCancellationTokenSource.Dispose();
            _logsSubscription?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            _filter = newFilter;
        }
    }

    private void HandleClear()
    {
        _filter = string.Empty;
    }

    private void ViewErrorStructuredLogs(ResourceViewModel resource)
    {
        NavigationManager.NavigateTo($"/StructuredLogs/{resource.Uid}?level=error");
    }
}
