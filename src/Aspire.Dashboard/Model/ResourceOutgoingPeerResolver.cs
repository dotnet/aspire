// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class ResourceOutgoingPeerResolver : IOutgoingPeerResolver, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _watchContainersTokenSource = new();
    private readonly List<ModelSubscription> _subscriptions = [];
    private readonly object _lock = new();
    private readonly Task? _watchTask;

    public ResourceOutgoingPeerResolver(IDashboardClient resourceService)
    {
        if (!resourceService.IsEnabled)
        {
            return;
        }

        var (snapshot, subscription) = resourceService.SubscribeResources();

        foreach (var resource in snapshot)
        {
            var added = _resourceByName.TryAdd(resource.Name, resource);
            Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
        }

        _watchTask = Task.Run(async () =>
        {
            await foreach (var (changeType, resource) in subscription.WithCancellation(_watchContainersTokenSource.Token))
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

                await RaisePeerChangesAsync().ConfigureAwait(false);
            }
        });
    }

    public bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, [NotNullWhen(true)] out string? name)
    {
        var address = OtlpHelpers.GetValue(attributes, OtlpSpan.PeerServiceAttributeKey);
        if (address != null)
        {
            foreach (var (resourceName, resource) in _resourceByName)
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
            var subscription = new ModelSubscription(callback, RemoveSubscription);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    private void RemoveSubscription(ModelSubscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private async Task RaisePeerChangesAsync()
    {
        if (_subscriptions.Count == 0 || _watchContainersTokenSource.IsCancellationRequested)
        {
            return;
        }

        ModelSubscription[] subscriptions;
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

        if (_watchTask is not null)
        {
            try
            {
                await _watchTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
