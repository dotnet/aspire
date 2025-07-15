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
                var hasPeerRelevantChanges = false;

                foreach (var (changeType, resource) in changes)
                {
                    if (changeType == ResourceViewModelChangeType.Upsert)
                    {
                        if (!_resourceByName.TryGetValue(resource.Name, out var existingResource) || 
                            !ArePeerRelevantPropertiesEquivalent(resource, existingResource))
                        {
                            hasPeerRelevantChanges = true;
                        }

                        _resourceByName[resource.Name] = resource;
                    }
                    else if (changeType == ResourceViewModelChangeType.Delete)
                    {
                        hasPeerRelevantChanges = true;

                        var removed = _resourceByName.TryRemove(resource.Name, out _);
                        Debug.Assert(removed, "Cannot remove unknown resource.");
                    }
                }

                if (hasPeerRelevantChanges)
                {
                    await RaisePeerChangesAsync().ConfigureAwait(false);
                }
            }
        });
    }

    private static bool ArePeerRelevantPropertiesEquivalent(ResourceViewModel resource1, ResourceViewModel resource2)
    {
        // Check if URLs are equivalent
        if (!AreUrlsEquivalent(resource1.Urls, resource2.Urls))
        {
            return false;
        }

        // Check if connection string properties are equivalent
        if (!ArePropertyValuesEquivalent(resource1, resource2, KnownProperties.Resource.ConnectionString))
        {
            return false;
        }

        // Check if parameter value properties are equivalent
        if (!ArePropertyValuesEquivalent(resource1, resource2, KnownProperties.Parameter.Value))
        {
            return false;
        }

        return true;
    }

    private static bool AreUrlsEquivalent(ImmutableArray<UrlViewModel> urls1, ImmutableArray<UrlViewModel> urls2)
    {
        // Compare if the two sets of URLs are equivalent.
        if (urls1.Length != urls2.Length)
        {
            return false;
        }

        for (var i = 0; i < urls1.Length; i++)
        {
            var url1 = urls1[i].Url;
            var url2 = urls2[i].Url;

            if (!url1.Equals(url2))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ArePropertyValuesEquivalent(ResourceViewModel resource1, ResourceViewModel resource2, string propertyName)
    {
        var hasProperty1 = resource1.Properties.TryGetValue(propertyName, out var property1);
        var hasProperty2 = resource2.Properties.TryGetValue(propertyName, out var property2);

        // If both don't have the property, they're equivalent
        if (!hasProperty1 && !hasProperty2)
        {
            return true;
        }

        // If only one has the property, they're not equivalent
        if (hasProperty1 != hasProperty2)
        {
            return false;
        }

        // Both have the property, compare values
        var value1 = property1!.Value.TryConvertToString(out var str1) ? str1 : string.Empty;
        var value2 = property2!.Value.TryConvertToString(out var str2) ? str2 : string.Empty;

        return string.Equals(value1, value2, StringComparison.Ordinal);
    }

    public bool TryResolvePeer(KeyValuePair<string, string>[] attributes, out string? name, out ResourceViewModel? matchedResource)
    {
        return TryResolvePeerCore(_resourceByName, attributes, out name, out matchedResource);
    }

    internal static bool TryResolvePeerCore(IDictionary<string, ResourceViewModel> resources, KeyValuePair<string, string>[] attributes, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out ResourceViewModel? resourceMatch)
    {
        var address = OtlpHelpers.GetPeerAddress(attributes);
        if (address != null)
        {
            // Apply transformers to the peer address cumulatively
            var transformedAddress = address;
            
            // First check exact match
            if (TryMatchAgainstResources(transformedAddress, resources, out name, out resourceMatch))
            {
                return true;
            }
            
            // Then apply each transformer cumulatively and check
            foreach (var transformer in s_addressTransformers)
            {
                transformedAddress = transformer(transformedAddress);
                if (TryMatchAgainstResources(transformedAddress, resources, out name, out resourceMatch))
                {
                    return true;
                }
            }
        }

        name = null;
        resourceMatch = null;
        return false;
    }

    /// <summary>
    /// Checks if a transformed peer address matches any of the resource addresses using their cached addresses.
    /// Applies the same transformations to resource addresses for consistent matching.
    /// Returns true only if exactly one resource matches; false if no matches or multiple matches are found.
    /// </summary>
    private static bool TryMatchAgainstResources(string peerAddress, IDictionary<string, ResourceViewModel> resources, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out ResourceViewModel? resourceMatch)
    {
        ResourceViewModel? foundResource = null;

        foreach (var (_, resource) in resources)
        {
            foreach (var resourceAddress in resource.CachedAddresses)
            {
                if (DoesAddressMatch(resourceAddress, peerAddress))
                {
                    if (foundResource is null)
                    {
                        foundResource = resource;
                    }
                    else if (!string.Equals(foundResource.Name, resource.Name, StringComparisons.ResourceName))
                    {
                        // Multiple different resources match - return false immediately
                        name = null;
                        resourceMatch = null;
                        return false;
                    }
                    break; // No need to check other addresses for this resource once we found a match
                }
            }
        }

        // Return true only if exactly one resource matched
        if (foundResource is not null)
        {
            name = ResourceViewModel.GetResourceName(foundResource, resources);
            resourceMatch = foundResource;
            return true;
        }

        // Return false if no matches found
        name = null;
        resourceMatch = null;
        return false;
    }

    private static bool DoesAddressMatch(string endpoint, string value)
    {
        if (string.Equals(endpoint, value, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Apply the same transformations that are applied to the peer service value
        var transformedEndpoint = endpoint;
        foreach (var transformer in s_addressTransformers)
        {
            transformedEndpoint = transformer(transformedEndpoint);
            if (string.Equals(transformedEndpoint, value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
