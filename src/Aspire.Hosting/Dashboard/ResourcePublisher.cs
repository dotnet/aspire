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
internal sealed class ResourcePublisher : IDisposable
{
    private readonly object _syncLock = new();
    private bool _disposed;
    private readonly Dictionary<string, ResourceSnapshot> _snapshot = [];
    // Internal for testing
    internal ImmutableHashSet<Channel<ResourceSnapshotChange>> _outgoingChannels = [];

    internal bool TryGetResource(string resourceName, [NotNullWhen(returnValue: true)] out ResourceSnapshot? resource)
    {
        lock (_syncLock)
        {
            return _snapshot.TryGetValue(resourceName, out resource);
        }
    }

    public ResourceSnapshotSubscription Subscribe()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = Channel.CreateUnbounded<ResourceSnapshotChange>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

        ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

        return new ResourceSnapshotSubscription(
            InitialState: _snapshot.Values.ToImmutableArray(),
            Subscription: StreamUpdates());

        async IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> StreamUpdates([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var batch in channel.GetBatchesAsync(cancellationToken).ConfigureAwait(false))
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

    /// <summary>
    /// Integrates a changed resource within the cache, and broadcasts the update to any subscribers.
    /// </summary>
    /// <param name="resource">The resource that was modified.</param>
    /// <param name="changeType">The change type (Added, Modified, Deleted).</param>
    /// <returns>A task that completes when the cache has been updated and all subscribers notified.</returns>
    internal async ValueTask IntegrateAsync(ResourceSnapshot resource, ResourceSnapshotChangeType changeType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        }

        // The publisher could be disposed while writing. WriteAsync will throw ChannelClosedException.
        foreach (var channel in _outgoingChannels)
        {
            await channel.Writer.WriteAsync(new(changeType, resource)).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _disposed = true;

        foreach (var item in _outgoingChannels)
        {
            item.Writer.Complete();
        }

        _outgoingChannels = _outgoingChannels.Clear();
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
