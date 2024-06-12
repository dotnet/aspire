// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public class ResourceNotificationService(ILogger<ResourceNotificationService> logger)
{
    private static readonly TimeSpan s_defaultWaitForResourceStateTimeout = TimeSpan.FromSeconds(60);
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly CancellationToken _applicationStopping;

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostApplicationLifetime">The host application lifetime.</param>
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime)
        : this(logger)
    {
        ArgumentNullException.ThrowIfNull(hostApplicationLifetime);

        _applicationStopping = hostApplicationLifetime.ApplicationStopping;
    }

    /// <summary>
    /// Waits for a resource to reach the specified state. See <see cref="KnownResourceStates"/> for common states.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the resource reaches the specified state. If the resource is already in the target state, the method will return immediately.<br/>
    /// If the resource never reaches the target state, the method will wait for the passed <see cref="CancellationToken"/> to be cancelled, or 60 seconds if <c>null</c>, and then throw <see cref="TaskCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resouce.</param>
    /// <param name="targetState">The state to wait for the resource to transition to. See <see cref="KnownResourceStates"/> for common states. Defaults to <see cref="KnownResourceStates.Running"/>.</param>
    /// <param name="cancellationToken">The cancellation token to monitor to cancel the wait operation. If <c>null</c> defaults to waiting for 60 seconds.</param>
    /// <returns>A <see cref="Task"/> representing the wait operation.</returns>
    public async Task WaitForResourceAsync(string resourceName, string? targetState = null, CancellationToken? cancellationToken = null)
    {
        var stateToWaitFor = targetState ?? KnownResourceStates.Running;
        CancellationTokenSource? timeoutCts = null;
        CancellationToken token;
        if (cancellationToken is { } ct)
        {
            token = ct;
        }
        else
        {
            timeoutCts = new(s_defaultWaitForResourceStateTimeout);
            token = timeoutCts.Token;
        }
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping, token);
        try
        {
            await foreach (var resourceEvent in WatchAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName)
                    && string.Equals(stateToWaitFor, resourceEvent.Snapshot.State?.Text, StringComparisons.ResourceState))
                {
                    return;
                }
            }
        }
        finally
        {
            timeoutCts?.Dispose();
            cts.Dispose();
        }
    }

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
                var previousStateText = previousState?.State?.Text;
                if (!string.IsNullOrEmpty(previousStateText))
                {
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {PreviousState} -> {NewState}", resource.Name, resourceId, previousStateText, newState.State?.Text);
                }
                else
                {
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {NewState}", resource.Name, resourceId, newState.State?.Text);
                }
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
