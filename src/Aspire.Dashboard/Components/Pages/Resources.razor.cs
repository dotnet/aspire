// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class Resources : ComponentBase, IDisposable
{
    private Subscription? _logsSubscription;
    private Dictionary<OtlpApplication, int>? _applicationUnviewedErrorCounts;

    [Inject]
    public required IResourceService ResourceService { get; init; }
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    private IEnumerable<EnvironmentVariableViewModel>? SelectedEnvironmentVariables { get; set; }
    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly Dictionary<string, ResourceViewModel> _resourcesMap = [];
    // TODO populate resource types from server data
    private readonly ImmutableArray<string> _allResourceTypes = ["Project", "Executable", "Container"];
    private readonly HashSet<string> _visibleResourceTypes;
    private string _filter = "";
    private bool _isTypeFilterVisible;

    public Resources()
    {
        _visibleResourceTypes = new HashSet<string>(_allResourceTypes, StringComparers.ResourceType);
    }

    private bool Filter(ResourceViewModel resource) => _visibleResourceTypes.Contains(resource.ResourceType) && (_filter.Length == 0 || resource.MatchesFilter(_filter));

    protected void OnResourceTypeVisibilityChanged(string resourceType, bool isVisible)
    {
        if (isVisible)
        {
            _visibleResourceTypes.Add(resourceType);
        }
        else
        {
            _visibleResourceTypes.Remove(resourceType);
        }
    }

    private bool? AreAllTypesVisible
    {
        get
        {
            return _visibleResourceTypes.SetEquals(_allResourceTypes)
                ? true
                : _visibleResourceTypes.Count == 0
                    ? false
                    : null;
        }
        set
        {
            if (value is true)
            {
                _visibleResourceTypes.UnionWith(_allResourceTypes);
            }
            else if (value is false)
            {
                _visibleResourceTypes.Clear();
            }
        }
    }

    private IQueryable<ResourceViewModel>? FilteredResources => _resourcesMap.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);

    protected override void OnInitialized()
    {
        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();

        var (snapshot, subscription) = ResourceService.Subscribe();

        foreach (var resource in snapshot)
        {
            _resourcesMap.Add(resource.Name, resource);
        }

        _ = Task.Run(async () =>
        {
            await foreach (var (changeType, resource) in subscription.WithCancellation(_watchTaskCancellationTokenSource.Token))
            {
                await OnResourceListChanged(changeType, resource);
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
        if (SelectedResource == resource)
        {
            ClearSelectedResource();
        }
        else
        {
            SelectedEnvironmentVariables = resource.Environment;
            SelectedResource = resource;
        }
    }

    private void ClearSelectedResource()
    {
        SelectedEnvironmentVariables = null;
        SelectedResource = null;
    }

    private async Task OnResourceListChanged(ResourceChangeType changeType, ResourceViewModel resource)
    {
        switch (changeType)
        {
            case ResourceChangeType.Upsert:
                _resourcesMap[resource.Name] = resource;
                break;

            case ResourceChangeType.Delete:
                _resourcesMap.Remove(resource.Name);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourcesMap.Values);

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

    private string? GetRowClass(ResourceViewModel resource)
        => resource == SelectedResource ? "selected-row" : null;
}
