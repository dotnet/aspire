// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public class ResourceNotificationService(ILogger<ResourceNotificationService> logger)
{
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    /// <summary>
    /// Watch for changes to the state for all resources.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<ResourceEvent> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Return the last snapshot for each resource.
        foreach (var state in _resourceNotificationStates)
        {
            var (resource, resourceId) = state.Key;

            if (state.Value.LastSnapshot is not null)
            {
                yield return new ResourceEvent(resource, resourceId, state.Value.LastSnapshot);
            }
        }

        var channel = Channel.CreateUnbounded<ResourceEvent>();

        void WriteToChannel(ResourceEvent resourceEvent) =>
            channel.Writer.TryWrite(resourceEvent);

        OnResourceUpdated += WriteToChannel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            OnResourceUpdated -= WriteToChannel;

            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="resourceId"> The id of the resource.</param>
    /// <param name="stateFactory">A factory that creates the new state based on the previous state.</param>
    public Task PublishUpdateAsync(IResource resource, string resourceId, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        var notificationState = GetResourceNotificationState(resource, resourceId);

        lock (notificationState)
        {
            var previousState = GetCurrentSnapshot(resource, notificationState);

            var newState = stateFactory(previousState);

            notificationState.LastSnapshot = newState;

            OnResourceUpdated?.Invoke(new ResourceEvent(resource, resourceId, newState));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Resource {Resource}/{ResourceId} -> {State}", resource.Name, resourceId, newState.State);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="stateFactory">A factory that creates the new state based on the previous state.</param>
    public Task PublishUpdateAsync(IResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        return PublishUpdateAsync(resource, resource.Name, stateFactory);
    }

    private static CustomResourceSnapshot GetCurrentSnapshot(IResource resource, ResourceNotificationState notificationState)
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

    private ResourceNotificationState GetResourceNotificationState(IResource resource, string resourceId) =>
        _resourceNotificationStates.GetOrAdd((resource, resourceId), _ => new ResourceNotificationState());

    /// <summary>
    /// The annotation that allows publishing and subscribing to changes in the state of a resource.
    /// </summary>
    private sealed class ResourceNotificationState
    {
        public CustomResourceSnapshot? LastSnapshot { get; set; }
    }
}

/// <summary>
/// Represents a change in the state of a resource.
/// </summary>
/// <param name="resource">The resource associated with the event.</param>
/// <param name="resourceId">The unique id of the resource.</param>
/// <param name="snapshot">The snapshot of the resource state.</param>
public class ResourceEvent(IResource resource, string resourceId, CustomResourceSnapshot snapshot)
{
    /// <summary>
    /// The resource associated with the event.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The unique id of the resource.
    /// </summary>
    public string ResourceId { get; } = resourceId;

    /// <summary>
    /// The snapshot of the resource state.
    /// </summary>
    public CustomResourceSnapshot Snapshot { get; } = snapshot;
}
