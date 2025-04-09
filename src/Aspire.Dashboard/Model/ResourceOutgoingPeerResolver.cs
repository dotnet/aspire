// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Utils;

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

        _watchTask = Task.Run(async () =>
        {
            var (snapshot, subscription) = await resourceService.SubscribeResourcesAsync(_watchContainersTokenSource.Token).ConfigureAwait(false);

            if (snapshot.Length > 0)
            {
                foreach (var resource in snapshot)
                {
                    var added = _resourceByName.TryAdd(resource.Name, resource);
                    Debug.Assert(added, "Should not receive duplicate resources in initial snapshot data.");
                }

                await RaisePeerChangesAsync().ConfigureAwait(false);
            }

            await foreach (var changes in subscription.WithCancellation(_watchContainersTokenSource.Token).ConfigureAwait(false))
            {
                var hasUrlChanges = false;

                foreach (var (changeType, resource) in changes)
                {
                    if (changeType == ResourceViewModelChangeType.Upsert)
                    {
                        if (!_resourceByName.TryGetValue(resource.Name, out var existingResource) || !AreEquivalent(resource.Urls, existingResource.Urls))
                        {
                            hasUrlChanges = true;
                        }

                        _resourceByName[resource.Name] = resource;
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        hasUrlChanges = true;

                        var removed = _resourceByName.TryRemove(resource.Name, out _);
                        Debug.Assert(removed, "Cannot remove unknown resource.");
                    }
                }

                if (hasUrlChanges)
                {
                    await RaisePeerChangesAsync().ConfigureAwait(false);
                }
            }
        });
    }

    private static bool AreEquivalent(ImmutableArray<UrlViewModel> urls1, ImmutableArray<UrlViewModel> urls2)
    {
        // Compare if the two sets of URLs are equivalent.
        if (urls1.Length != urls2.Length)
        {
            return false;
        }

        for (var i = 0; i < urls1.Length; i++)
        {
            if (!urls1[i].Equals(urls2[i]))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, out string? name, out ResourceViewModel? matchedResource)
    {
        return TryResolvePeerNameCore(_resourceByName, attributes, out name, out matchedResource);
    }

    internal static bool TryResolvePeerNameCore(IDictionary<string, ResourceViewModel> resources, KeyValuePair<string, string>[] attributes, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out ResourceViewModel? resourceMatch)
    {
        var address = OtlpHelpers.GetPeerAddress(attributes);
        if (address != null)
        {
            // Match exact value.
            if (TryMatchResourceAddress(address, out name, out resourceMatch))
            {
                return true;
            }

            // Resource addresses have the format "127.0.0.1:5000". Some libraries modify the peer.service value on the span.
            // If there isn't an exact match then transform the peer.service value and try to match again.
            // Change from transformers are cumulative. e.g. "localhost,5000" -> "localhost:5000" -> "127.0.0.1:5000"
            var transformedAddress = address;
            foreach (var transformer in s_addressTransformers)
            {
                transformedAddress = transformer(transformedAddress);
                if (TryMatchResourceAddress(transformedAddress, out name, out resourceMatch))
                {
                    return true;
                }
            }
        }

        name = null;
        resourceMatch = null;
        return false;

        bool TryMatchResourceAddress(string value, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out ResourceViewModel? resourceMatch)
        {
            foreach (var (resourceName, resource) in resources)
            {
                foreach (var service in resource.Urls)
                {
                    var hostAndPort = service.Url.GetComponents(UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped);

                    if (string.Equals(hostAndPort, value, StringComparison.OrdinalIgnoreCase))
                    {
                        name = ResourceViewModel.GetResourceName(resource, resources);
                        resourceMatch = resource;
                        return true;
                    }
                }
            }

            name = null;
            resourceMatch = null;
            return false;
        }
    }

    private static readonly List<Func<string, string>> s_addressTransformers = [
        s =>
        {
            // SQL Server uses comma instead of colon for port.
            // https://www.connectionstrings.com/sql-server/
            if (s.AsSpan().Count(',') == 1)
            {
                return s.Replace(',', ':');
            }
            return s;
        },
        s =>
        {
            // Some libraries use "127.0.0.1" instead of "localhost".
            return s.Replace("127.0.0.1:", "localhost:");
        }];

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

        await TaskHelpers.WaitIgnoreCancelAsync(_watchTask).ConfigureAwait(false);
    }
}
