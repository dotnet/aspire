// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public abstract class ResourcesListBase<TResource> : ComponentBase, IDisposable
    where TResource : ResourceViewModel
{
    // Ideally we'd be pulling this from Aspire.Hosting.Dcp.Model.ExecutableStates,
    // but unfortunately the reference goes the other way
    protected const string FinishedState = "Finished";

    private Subscription? _logsSubscription;
    private Dictionary<OtlpApplication, int>? _applicationUnviewedErrorCounts;

    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; init; }
    [Inject]
    public required EnvironmentVariablesDialogService EnvironmentVariablesDialogService { get; init; }
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    protected abstract ViewModelMonitor<TResource> GetViewModelMonitor(IDashboardViewModelService dashboardViewModelService);
    protected abstract bool Filter(TResource resource);
    protected virtual bool ShowSpecOnlyToggle => true;

    private readonly Dictionary<string, TResource> _resourcesMap = new();
    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    protected string filter = "";

    protected IQueryable<TResource>? FilteredResources => _resourcesMap.Values.Where(Filter).OrderBy(e => e.Name).AsQueryable();

    protected GridSort<TResource> nameSort = GridSort<TResource>.ByAscending(p => p.Name);
    protected GridSort<TResource> stateSort = GridSort<TResource>.ByAscending(p => p.State);

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

    protected int GetUnviewedErrorCount(TResource resource)
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

    protected async Task ShowEnvironmentVariables(TResource resource)
    {
        await EnvironmentVariablesDialogService.ShowDialogAsync(
            source: resource.Name,
            viewModel: new()
            {
                EnvironmentVariables = resource.Environment,
                ShowSpecOnlyToggle = ShowSpecOnlyToggle
            }
        );
    }

    private async Task OnResourceListChanged(ObjectChangeType objectChangeType, TResource resource)
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

    protected void HandleFilter(ChangeEventArgs args)
    {
        if (args.Value is string newFilter)
        {
            filter = newFilter;
        }
    }

    protected void HandleClear(string? value)
    {
        filter = value ?? string.Empty;
    }

    protected void ViewErrorStructuredLogs(TResource resource)
    {
        NavigationManager.NavigateTo($"/StructuredLogs/{resource.Uid}?level=error");
    }
}
