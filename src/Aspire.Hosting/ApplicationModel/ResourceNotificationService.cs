// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public class ResourceNotificationService
{
    private readonly ConcurrentDictionary<string, ResourceNotificationState> _resourceNotificationStates = new();

    /// <summary>
    /// Watch for changes to the dashboard state for a resource.
    /// </summary>
    /// <param name="resource">The name of the resource</param>
    /// <returns></returns>
    public IAsyncEnumerable<CustomResourceSnapshot> WatchAsync(IResource resource)
    {
        var notificationState = GetResourceNotificationState(resource.Name);

        lock (notificationState)
        {
            // When watching a resource, make sure the initial snapshot is set.
            notificationState.LastSnapshot = GetInitialSnapshot(resource, notificationState);
        }

        return notificationState.WatchAsync();
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="stateFactory"></param>
    /// <returns></returns>
    public Task PublishUpdateAsync(IResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        var notificationState = GetResourceNotificationState(resource.Name);

        lock (notificationState)
        {
            var previousState = GetInitialSnapshot(resource, notificationState);

            var newState = stateFactory(previousState!);

            notificationState.LastSnapshot = newState;

            return notificationState.PublishUpdateAsync(newState);
        }
    }

    private static CustomResourceSnapshot? GetInitialSnapshot(IResource resource, ResourceNotificationState notificationState)
    {
        var previousState = notificationState.LastSnapshot;

        if (previousState is null)
        {
            if (resource.Annotations.OfType<ResourceSnapshotAnnotation>().LastOrDefault() is { } annotation)
            {
                previousState = annotation.InitialSnapshot;
            }

            // If there is no initial snapshot, create an empty one.
            previousState ??= new CustomResourceSnapshot()
            {
                ResourceType = resource.GetType().Name,
                Properties = []
            };
        }

        return previousState;
    }

    /// <summary>
    /// Signal that no more updates are expected for this resource.
    /// </summary>
    public void Complete(IResource resource)
    {
        if (_resourceNotificationStates.TryGetValue(resource.Name, out var state))
        {
            state.Complete();
        }
    }

    private ResourceNotificationState GetResourceNotificationState(string resourceName) =>
        _resourceNotificationStates.GetOrAdd(resourceName, _ => new ResourceNotificationState());

    /// <summary>
    /// The annotation that allows publishing and subscribing to changes in the state of a resource.
    /// </summary>
    private sealed class ResourceNotificationState
    {
        private readonly CancellationTokenSource _streamClosedCts = new();

        private Action<CustomResourceSnapshot>? OnSnapshotUpdated { get; set; }

        public CustomResourceSnapshot? LastSnapshot { get; set; }

        /// <summary>
        /// Watch for changes to the dashboard state for a resource.
        /// </summary>
        public IAsyncEnumerable<CustomResourceSnapshot> WatchAsync() => new ResourceUpdatesAsyncEnumerable(this);

        /// <summary>
        /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
        /// </summary>
        /// <param name="state">The new <see cref="CustomResourceSnapshot"/>.</param>
        public Task PublishUpdateAsync(CustomResourceSnapshot state)
        {
            if (_streamClosedCts.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            OnSnapshotUpdated?.Invoke(state);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Signal that no more updates are expected for this resource.
        /// </summary>
        public void Complete()
        {
            _streamClosedCts.Cancel();
        }

        private sealed class ResourceUpdatesAsyncEnumerable(ResourceNotificationState customResourceAnnotation) : IAsyncEnumerable<CustomResourceSnapshot>
        {
            public async IAsyncEnumerator<CustomResourceSnapshot> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                if (customResourceAnnotation.LastSnapshot is not null)
                {
                    yield return customResourceAnnotation.LastSnapshot;
                }

                var channel = Channel.CreateUnbounded<CustomResourceSnapshot>();

                void WriteToChannel(CustomResourceSnapshot state)
                    => channel.Writer.TryWrite(state);

                using var _ = customResourceAnnotation._streamClosedCts.Token.Register(() => channel.Writer.TryComplete());

                customResourceAnnotation.OnSnapshotUpdated = WriteToChannel;

                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        yield return item;
                    }
                }
                finally
                {
                    customResourceAnnotation.OnSnapshotUpdated -= WriteToChannel;

                    channel.Writer.TryComplete();
                }
            }
        }
    }
}
