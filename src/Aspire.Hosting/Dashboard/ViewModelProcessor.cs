// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.Dashboard;

internal sealed class ViewModelProcessor
{
    private readonly object _syncLock = new();
    private readonly Channel<ResourceChange> _incomingChannel;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, ResourceViewModel> _snapshot = [];
    private readonly List<Channel<ResourceChange>> _subscribedChannels = [];

    public ViewModelProcessor(Channel<ResourceChange> incomingChannel, CancellationToken cancellationToken)
    {
        _incomingChannel = incomingChannel;
        _cancellationToken = cancellationToken;

        Task.Run(ProcessChanges, cancellationToken);
    }

    public ViewModelMonitor GetMonitor()
    {
        lock (_syncLock)
        {
            var channel = Channel.CreateUnbounded<ResourceChange>();
            _subscribedChannels.Add(channel);

            return new ViewModelMonitor(
                Snapshot: _snapshot.Values.ToList(),
                Watch: new ChangeEnumerable(channel, RemoveChannel));
        }
    }

    private void RemoveChannel(Channel<ResourceChange> channel)
    {
        lock (_syncLock)
        {
            _subscribedChannels.Remove(channel);
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
            List<Channel<ResourceChange>> outgoingChannels;
            lock (_syncLock)
            {
                var resource = change.Resource;
                switch (change.ObjectChangeType)
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

                outgoingChannels = _subscribedChannels.ToList();
            }

            foreach (var channel in outgoingChannels)
            {
                await channel.Writer.WriteAsync(change, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
