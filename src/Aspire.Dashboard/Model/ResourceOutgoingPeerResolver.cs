// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ResourceOutgoingPeerResolver : IOutgoingPeerResolver, IAsyncDisposable
{
    private readonly IDashboardViewModelService _dashboardViewModelService;
    private readonly Dictionary<string, ResourceViewModel> _resourceNameMapping = new();
    private readonly CancellationTokenSource _watchContainersTokenSource = new();
    private readonly Task _watchTask;
    private readonly object _lock = new object();

    public ResourceOutgoingPeerResolver(IDashboardViewModelService dashboardViewModelService)
    {
        _dashboardViewModelService = dashboardViewModelService;

        var viewModelMonitor = _dashboardViewModelService.GetResources();
        var initialList = viewModelMonitor.Snapshot;
        var watch = viewModelMonitor.Watch;

        foreach (var result in initialList)
        {
            _resourceNameMapping[result.Name] = result;
        }

        _watchTask = Task.Run(async () =>
        {
            await foreach (var resourceChanged in watch.WithCancellation(_watchContainersTokenSource.Token))
            {
                OnResourceListChanged(resourceChanged.ObjectChangeType, resourceChanged.Resource);
            }
        });
    }

    private void OnResourceListChanged(ObjectChangeType changeType, ResourceViewModel resourceViewModel)
    {
        lock (_lock)
        {
            if (changeType == ObjectChangeType.Added)
            {
                _resourceNameMapping[resourceViewModel.Name] = resourceViewModel;
            }
            else if (changeType == ObjectChangeType.Modified)
            {
                _resourceNameMapping[resourceViewModel.Name] = resourceViewModel;
            }
            else if (changeType == ObjectChangeType.Deleted)
            {
                _resourceNameMapping.Remove(resourceViewModel.Name);
            }
        }
    }

    public string ResolvePeerName(string networkAddress)
    {
        lock (_lock)
        {
            foreach (var resource in _resourceNameMapping.Values)
            {
                foreach (var service in resource.Services)
                {
                    if (service.AddressAndPort == networkAddress)
                    {
                        return resource.Name;
                    }
                }
            }
        }

        return networkAddress;
    }

    public async ValueTask DisposeAsync()
    {
        _watchContainersTokenSource.Cancel();
        _watchContainersTokenSource.Dispose();

        try
        {
            await _watchTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
