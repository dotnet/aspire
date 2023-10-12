// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Components.Pages;

public abstract class ResourcesListBase<TResource> : ComponentBase
    where TResource : ResourceViewModel
{
    [Inject]
    public required IDashboardViewModelService DashboardViewModelService { get; init; }
    [Inject]
    public required EnvironmentVariablesDialogService EnvironmentVariablesDialogService { get; init; }

    protected abstract Task<List<TResource>> GetResources(IDashboardViewModelService dashboardViewModelService);
    protected abstract IAsyncEnumerable<ResourceChanged<TResource>> WatchResources(
        IDashboardViewModelService dashboardViewModelService,
        IEnumerable<NamespacedName> initialList,
        CancellationToken cancellationToken);
    protected abstract bool Filter(TResource resource);
    protected virtual bool ShowSpecOnlyToggle => true;

    private readonly Dictionary<string, TResource> _resourcesMap = new();
    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    protected string filter = "";

    protected IQueryable<TResource>? FilteredResources => _resourcesMap.Values.Where(Filter).OrderBy(e => e.Name).AsQueryable();

    protected GridSort<TResource> nameSort = GridSort<TResource>.ByAscending(p => p.Name);

    protected override async Task OnInitializedAsync()
    {
        var resources = await GetResources(DashboardViewModelService);
        foreach (var resource in resources)
        {
            _resourcesMap.Add(resource.Name, resource);
        }

        _ = Task.Run(async () =>
        {
            await foreach (var resourceChanged in WatchResources(
                DashboardViewModelService, resources.Select(e => e.NamespacedName), _watchTaskCancellationTokenSource.Token))
            {
                await OnResourceListChanged(resourceChanged.ObjectChangeType, resourceChanged.Resource);
            }
        });
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

    public void Dispose()
    {
        _watchTaskCancellationTokenSource.Cancel();
        _watchTaskCancellationTokenSource.Dispose();
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
}
