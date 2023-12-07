// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Builds a collection of resources by integrating incoming changes from a channel,
/// and allowing multiple subscribers to receive the current resource snapshot and future
/// updates.
/// </summary>
internal sealed class ResourceCollection
{
    private readonly object _syncLock = new();
    private readonly Channel<ResourceChange> _incomingChannel;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, ResourceViewModel> _snapshot = [];
    private ImmutableHashSet<Channel<ResourceChange>> _outgoingChannels = [];

    public ResourceCollection(Channel<ResourceChange> incomingChannel, CancellationToken cancellationToken)
    {
        _incomingChannel = incomingChannel;
        _cancellationToken = cancellationToken;

        Task.Run(ProcessChanges, cancellationToken);
    }

    public ResourceSubscription Subscribe()
    {
        lock (_syncLock)
        {
            var channel = Channel.CreateUnbounded<ResourceChange>();

            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            return new ResourceSubscription(
                Snapshot: _snapshot.Values.ToList(),
                Subscription: new ChangeEnumerable(channel, RemoveChannel));
        }

        void RemoveChannel(Channel<ResourceChange> channel)
        {
            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
        }
    }

    private sealed class ChangeEnumerable : IAsyncEnumerable<ResourceChange>
    {
        private readonly Channel<ResourceChange> _channel;
        private readonly Action<Channel<ResourceChange>> _disposeAction;

        public ChangeEnumerable(Channel<ResourceChange> channel, Action<Channel<ResourceChange>> disposeAction)
        {
            _channel = channel;
            _disposeAction = disposeAction;
        }

        public IAsyncEnumerator<ResourceChange> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ChangeEnumerator(_channel, _disposeAction, cancellationToken);
        }
    }

    private sealed class ChangeEnumerator : IAsyncEnumerator<ResourceChange>
    {
        private readonly Channel<ResourceChange> _channel;
        private readonly Action<Channel<ResourceChange>> _disposeAction;
        private readonly CancellationToken _cancellationToken;

        public ChangeEnumerator(
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

    private async Task ProcessChanges()
    {
        await foreach (var change in _incomingChannel.Reader.ReadAllAsync(_cancellationToken))
        {
            var (changeType, resource) = change;

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
                await channel.Writer.WriteAsync(change, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
