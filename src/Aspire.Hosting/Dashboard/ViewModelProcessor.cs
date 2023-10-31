// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.Dashboard;

internal sealed class ViewModelProcessor<TViewModel>
    where TViewModel : ResourceViewModel
{
    private readonly object _syncLock = new();
    private readonly Channel<ResourceChanged<TViewModel>> _incomingChannel;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, TViewModel> _snapshot = [];
    private readonly List<Channel<ResourceChanged<TViewModel>>> _subscribedChannels = [];

    public ViewModelProcessor(Channel<ResourceChanged<TViewModel>> incomingChannel, CancellationToken cancellationToken)
    {
        _incomingChannel = incomingChannel;
        _cancellationToken = cancellationToken;
        Task.Run(ProcessChanges, cancellationToken);
    }

    public ViewModelMonitor<TViewModel> GetResourceMonitor()
    {
        lock (_syncLock)
        {
            var snapshot = _snapshot.Values.ToList();
            var channel = Channel.CreateUnbounded<ResourceChanged<TViewModel>>();
            _subscribedChannels.Add(channel);
            var enumerable = new ChangeEnumerable(channel, RemoveChannel);

            return new ViewModelMonitor<TViewModel>(snapshot, enumerable);
        }
    }

    private void RemoveChannel(Channel<ResourceChanged<TViewModel>> channel)
    {
        lock (_syncLock)
        {
            _subscribedChannels.Remove(channel);
        }
    }

    private sealed class ChangeEnumerable : IAsyncEnumerable<ResourceChanged<TViewModel>>
    {
        private readonly Channel<ResourceChanged<TViewModel>> _channel;
        private readonly Action<Channel<ResourceChanged<TViewModel>>> _disposeAction;

        public ChangeEnumerable(Channel<ResourceChanged<TViewModel>> channel, Action<Channel<ResourceChanged<TViewModel>>> disposeAction)
        {
            _channel = channel;
            _disposeAction = disposeAction;
        }

        public IAsyncEnumerator<ResourceChanged<TViewModel>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ChangeEnumerator(_channel, _disposeAction, cancellationToken);
        }
    }

    private sealed class ChangeEnumerator : IAsyncEnumerator<ResourceChanged<TViewModel>>
    {
        private readonly Channel<ResourceChanged<TViewModel>> _channel;
        private readonly Action<Channel<ResourceChanged<TViewModel>>> _disposeAction;
        private readonly CancellationToken _cancellationToken;

        public ChangeEnumerator(
            Channel<ResourceChanged<TViewModel>> channel, Action<Channel<ResourceChanged<TViewModel>>> disposeAction, CancellationToken cancellationToken)
        {
            _channel = channel;
            _disposeAction = disposeAction;
            _cancellationToken = cancellationToken;
            Current = default!;
        }

        public ResourceChanged<TViewModel> Current { get; private set; }

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
            List<Channel<ResourceChanged<TViewModel>>> outgoingChannels;
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
