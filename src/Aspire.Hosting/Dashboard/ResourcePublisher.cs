// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Builds a collection of resources by integrating incoming resource changes,
/// and allowing multiple subscribers to receive the current resource collection
/// snapshot and future updates.
/// </summary>
internal sealed class ResourcePublisher(CancellationToken cancellationToken)
{
    private readonly object _syncLock = new();
    private readonly Dictionary<string, ResourceViewModel> _snapshot = [];
    private ImmutableHashSet<Channel<ResourceChange>> _outgoingChannels = [];

    public ResourceSubscription Subscribe()
    {
        lock (_syncLock)
        {
            var channel = Channel.CreateUnbounded<ResourceChange>();

            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            return new ResourceSubscription(
                Snapshot: [.. _snapshot.Values],
                Subscription: new ResourceSubscriptionEnumerable(channel, disposeAction: RemoveChannel));
        }

        void RemoveChannel(Channel<ResourceChange> channel)
        {
            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
        }
    }

    /// <summary>
    /// Integrates a changed resource within the cache, and broadcasts the update to any subscribers.
    /// </summary>
    /// <param name="resource">The resource that was modified.</param>
    /// <param name="changeType">The change type (Added, Modified, Deleted).</param>
    /// <returns>A task that completes when the cache has been updated and all subscribers notified.</returns>
    public async ValueTask Integrate(ResourceViewModel resource, ObjectChangeType changeType)
    {
        lock (_syncLock)
        {
            switch (changeType)
            {
                case ObjectChangeType.Added:
                    _snapshot.Add(resource.Name, resource);
                    break;

                case ObjectChangeType.Modified:
                    _snapshot[resource.Name] = resource;
                    break;

                case ObjectChangeType.Deleted:
                    _snapshot.Remove(resource.Name);
                    break;
            }
        }

        foreach (var channel in _outgoingChannels)
        {
            await channel.Writer.WriteAsync(new(changeType, resource), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ResourceSubscriptionEnumerable : IAsyncEnumerable<ResourceChange>
    {
        private readonly Channel<ResourceChange> _channel;
        private readonly Action<Channel<ResourceChange>> _disposeAction;

        public ResourceSubscriptionEnumerable(Channel<ResourceChange> channel, Action<Channel<ResourceChange>> disposeAction)
        {
            _channel = channel;
            _disposeAction = disposeAction;
        }

        public IAsyncEnumerator<ResourceChange> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ResourceSubscriptionEnumerator(_channel, _disposeAction, cancellationToken);
        }
    }

    private sealed class ResourceSubscriptionEnumerator : IAsyncEnumerator<ResourceChange>
    {
        private readonly Channel<ResourceChange> _channel;
        private readonly Action<Channel<ResourceChange>> _disposeAction;
        private readonly CancellationToken _cancellationToken;

        public ResourceSubscriptionEnumerator(
            Channel<ResourceChange> channel, Action<Channel<ResourceChange>> disposeAction, CancellationToken cancellationToken)
        {
            _channel = channel;
            _disposeAction = disposeAction;
            _cancellationToken = cancellationToken;
            Current = default!;
        }

        public ResourceChange Current { get; private set; }

        public ValueTask DisposeAsync()
        {
            _disposeAction(_channel);

            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            Current = await _channel.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);

            return true;
        }
    }
}
