// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class ResourceOutgoingPeerResolver : IOutgoingPeerResolver, IAsyncDisposable
{
    private readonly IDashboardViewModelService _dashboardViewModelService;
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceNameMapping = new();
    private readonly CancellationTokenSource _watchContainersTokenSource = new();
    private readonly Task _watchTask;
    private readonly List<Subscription> _subscriptions;
    private readonly object _lock = new object();

    public ResourceOutgoingPeerResolver(IDashboardViewModelService dashboardViewModelService)
    {
        _dashboardViewModelService = dashboardViewModelService;
        _subscriptions = new List<Subscription>();

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
                await OnResourceListChanged(resourceChanged.ObjectChangeType, resourceChanged.Resource).ConfigureAwait(false);
            }
        });
    }

    private async Task OnResourceListChanged(ObjectChangeType changeType, ResourceViewModel resourceViewModel)
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
            _resourceNameMapping.TryRemove(resourceViewModel.Name, out _);
        }

        await RaisePeerChangesAsync().ConfigureAwait(false);
    }

    public bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, [NotNullWhen(true)] out string? name)
    {
        var address = OtlpHelpers.GetValue(attributes, OtlpSpan.PeerServiceAttributeKey);
        if (address != null)
        {
            foreach (var (resourceName, resource) in _resourceNameMapping)
            {
                foreach (var service in resource.Services)
                {
                    if (string.Equals(service.AddressAndPort, address, StringComparison.OrdinalIgnoreCase))
                    {
                        name = resource.Name;
                        return true;
                    }
                }
            }
        }

        name = null;
        return false;
    }

    public IDisposable OnPeerChanges(Func<Task> callback)
    {
        lock (_lock)
        {
            var subscription = new Subscription(callback, RemoveSubscription);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    private void RemoveSubscription(Subscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private async Task RaisePeerChangesAsync()
    {
        if (_subscriptions.Count == 0)
        {
            return;
        }

        Subscription[] subscriptions;
        lock (_lock)
        {
            subscriptions = _subscriptions.ToArray();
        }

        foreach (var subscription in subscriptions)
        {
            await subscription.ExecuteAsync().ConfigureAwait(false);
        }
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

    private sealed class Subscription(Func<Task> callback, Action<Subscription> onDispose) : IDisposable
    {
        private readonly Func<Task> _callback = callback;
        private readonly Action<Subscription> _onDispose = onDispose;

        public void Dispose() => _onDispose(this);
        public Task ExecuteAsync() => _callback();
    }
}
