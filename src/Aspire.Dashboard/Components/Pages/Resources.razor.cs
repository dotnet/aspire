// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
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
    public required IDashboardClient DashboardClient { get; init; }
    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    private ResourceViewModel? SelectedResource { get; set; }

    private readonly CancellationTokenSource _watchTaskCancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    // TODO populate resource types from server data
    private readonly ImmutableArray<string> _allResourceTypes = [KnownResourceTypes.Project, KnownResourceTypes.Executable, KnownResourceTypes.Container];
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

    private IQueryable<ResourceViewModel>? FilteredResources => _resourceByName.Values.Where(Filter).OrderBy(e => e.ResourceType).ThenBy(e => e.Name).AsQueryable();

    private readonly GridSort<ResourceViewModel> _nameSort = GridSort<ResourceViewModel>.ByAscending(p => p.Name);
    private readonly GridSort<ResourceViewModel> _stateSort = GridSort<ResourceViewModel>.ByAscending(p => p.State);

    protected override void OnInitialized()
    {
        _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();

        if (DashboardClient.IsEnabled)
        {
            SubscribeResources();
        }

        _logsSubscription = TelemetryRepository.OnNewLogs(null, SubscriptionType.Other, async () =>
        {
            _applicationUnviewedErrorCounts = TelemetryRepository.GetApplicationUnviewedErrorLogsCount();
            await InvokeAsync(StateHasChanged);
        });

        void SubscribeResources()
        {
            var (snapshot, subscription) = DashboardClient.SubscribeResources();

            // Apply snapshot.
            foreach (var resource in snapshot)
            {
                var added = _resourceByName.TryAdd(resource.Name, resource);
                Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
            }

            // Listen for updates and apply.
            _ = Task.Run(async () =>
            {
                await foreach (var (changeType, resource) in subscription.WithCancellation(_watchTaskCancellationTokenSource.Token))
                {
                    if (changeType == ResourceViewModelChangeType.Upsert)
                    {
                        _resourceByName[resource.Name] = resource;
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        var removed = _resourceByName.TryRemove(resource.Name, out _);
                        Debug.Assert(removed, "Cannot remove unknown resource.");
                    }

                    await InvokeAsync(StateHasChanged);
                }
            });
        }
    }

    private void ShowResourceDetails(ResourceViewModel resource)
    {
        if (SelectedResource == resource)
        {
            ClearSelectedResource();
        }
        else
        {
            SelectedResource = resource;
        }
    }

    private void ClearSelectedResource()
    {
        SelectedResource = null;
    }

    private string GetResourceName(ResourceViewModel resource) => ResourceViewModel.GetResourceName(resource, _resourceByName.Values);

    private bool HasMultipleReplicas(ResourceViewModel resource)
    {
        var count = 0;
        foreach (var item in _resourceByName.Values)
        {
            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    return true;
                }
            }
        }

        return false;
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

    private string? GetRowClass(ResourceViewModel resource)
        => resource == SelectedResource ? "selected-row resource-row" : "resource-row";
}
