// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.Extensions;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Builds a collection of resources by integrating incoming resource changes,
/// and allowing multiple subscribers to receive the current resource collection
/// snapshot and future updates.
/// </summary>
internal sealed class ResourcePublisher(CancellationToken cancellationToken)
{
    private readonly object _syncLock = new();
    private readonly Dictionary<string, ResourceSnapshot> _snapshot = [];
    private ImmutableHashSet<Channel<ResourceSnapshotChange>> _outgoingChannels = [];

    // For testing purposes
    internal int OutgoingSubscriberCount => _outgoingChannels.Count;

    internal bool TryGetResource(string resourceName, [NotNullWhen(returnValue: true)] out ResourceSnapshot? resource)
    {
        lock (_syncLock)
        {
            return _snapshot.TryGetValue(resourceName, out resource);
        }
    }

    public ResourceSnapshotSubscription Subscribe()
    {
        lock (_syncLock)
        {
            var channel = Channel.CreateUnbounded<ResourceSnapshotChange>(
                new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            return new ResourceSnapshotSubscription(
                InitialState: _snapshot.Values.ToImmutableArray(),
                Subscription: StreamUpdates());

            async IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> StreamUpdates([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(enumeratorCancellationToken, cancellationToken);

                try
                {
                    await foreach (var batch in channel.GetBatches(linked.Token).ConfigureAwait(false))
                    {
                        yield return batch;
                    }
                }
                finally
                {
                    ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
                }
            }
        }
    }

    /// <summary>
    /// Integrates a changed resource within the cache, and broadcasts the update to any subscribers.
    /// </summary>
    /// <param name="resource">The resource that was modified.</param>
    /// <param name="changeType">The change type (Added, Modified, Deleted).</param>
    /// <returns>A task that completes when the cache has been updated and all subscribers notified.</returns>
    internal async ValueTask IntegrateAsync(ResourceSnapshot resource, ResourceSnapshotChangeType changeType)
    {
        ImmutableHashSet<Channel<ResourceSnapshotChange>> channels;

        lock (_syncLock)
        {
            switch (changeType)
            {
                case ResourceSnapshotChangeType.Upsert:
                    _snapshot[resource.Name] = resource;
                    break;

                case ResourceSnapshotChangeType.Delete:
                    _snapshot.Remove(resource.Name);
                    break;
            }

            channels = _outgoingChannels;
        }

        foreach (var channel in channels)
        {
            await channel.Writer.WriteAsync(new(changeType, resource), cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed record ResourceSnapshotSubscription(
    ImmutableArray<ResourceSnapshot> InitialState,
    IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> Subscription);

internal sealed record ResourceSnapshotChange(
    ResourceSnapshotChangeType ChangeType,
    ResourceSnapshot Resource);

internal enum ResourceSnapshotChangeType
{
    /// <summary>
    /// The object was added if new, or updated if not.
    /// </summary>
    Upsert,

    /// <summary>
    /// The object was deleted.
    /// </summary>
    Delete
}
