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
public class ResourceNotificationService
{
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger;
    private readonly CancellationToken _applicationStopping;

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <remarks>
    /// Obsolete. Use the constructor that accepts an <see cref="ILogger{ResourceNotificationService}"/> and <see cref="IHostApplicationLifetime"/>.<br/>
    /// This constructor will be removed in the next major version of Aspire.
    /// </remarks>
    /// <param name="logger">The logger.</param>
    [Obsolete($"""
        {nameof(ResourceNotificationService)} now requires an {nameof(IHostApplicationLifetime)}.
        Use the constructor that accepts an {nameof(ILogger)}<{nameof(ResourceNotificationService)}> and {nameof(IHostApplicationLifetime)}.
        This constructor will be removed in the next major version of Aspire.
        """)]
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostApplicationLifetime">The host application lifetime.</param>
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationStopping = hostApplicationLifetime?.ApplicationStopping ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
    }

    /// <summary>
    /// Waits for a resource to reach the specified state. See <see cref="KnownResourceStates"/> for common states.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the resource reaches the specified target state. If the resource
    /// is already in the target state, the method will return immediately.<br/>
    /// If the resource doesn't reach one of the target states before <paramref name="cancellationToken"/> is signalled, this method
    /// will throw <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resouce.</param>
    /// <param name="targetState"></param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> </param>
    /// <returns>A <see cref="Task"/> representing the wait operation.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "targetState(s) parameters are mutually exclusive.")]
    public Task WaitForResourceAsync(string resourceName, string? targetState = null, CancellationToken cancellationToken = default)
    {
        string[] targetStates = !string.IsNullOrEmpty(targetState) ? [targetState] : [KnownResourceStates.Running];
        return WaitForResourceAsync(resourceName, targetStates, cancellationToken);
    }

    /// <summary>
    /// Waits for a resource to reach one of the specified states. See <see cref="KnownResourceStates"/> for common states.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the resource reaches one of the specified target states. If the resource
    /// is already in the target state, the method will return immediately.<br/>
    /// If the resource doesn't reach one of the target states before <paramref name="cancellationToken"/> is signalled, this method
    /// will throw <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="targetStates">The set of states to wait for the resource to transition to one of. See <see cref="KnownResourceStates"/> for common states.</param>
    /// <param name="cancellationToken">A cancellation token that cancels the wait operation when signalled.</param>
    /// <returns>A <see cref="Task{String}"/> representing the wait operation and which of the target states the resource reached.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "targetState(s) parameters are mutually exclusive.")]
    public async Task<string> WaitForResourceAsync(string resourceName, IEnumerable<string> targetStates, CancellationToken cancellationToken = default)
    {
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping, cancellationToken);
        var watchToken = watchCts.Token;
        await foreach (var resourceEvent in WatchAsync(watchToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName)
                && resourceEvent.Snapshot.State?.Text is { Length: > 0 } statusText
                && targetStates.Contains(statusText, StringComparers.ResourceState))
            {
                return statusText;
            }
        }

        throw new OperationCanceledException($"The operation was cancelled before the resource reached one of the target states: [{string.Join(", ", targetStates)}]");
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

            if (_logger.IsEnabled(LogLevel.Debug) && newState.State?.Text is { Length: > 0 } newStateText)
            {
                var previousStateText = previousState?.State?.Text;
                if (!string.IsNullOrEmpty(previousStateText) && !string.Equals(previousStateText, newStateText, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {PreviousState} -> {NewState}", resource.Name, resourceId, previousStateText, newStateText);
                }
                else
                {
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {NewState}", resource.Name, resourceId, newStateText);
                }
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Resource {Resource}/{ResourceId} update published: " +
                    "ResourceType = {ResourceType}, CreationTimeStamp = {CreationTimeStamp}, State = {{ Text = {StateText}, Style = {StateStyle} }}, " +
                    "ExitCode = {ExitCode}, EnvironmentVariables = {{ {EnvironmentVariables} }}, Urls = {{ {Urls} }}, " +
                    "Properties = {{ {Properties} }}",
                    resource.Name, resourceId,
                    newState.ResourceType, newState.CreationTimeStamp, newState.State?.Text, newState.State?.Style,
                    newState.ExitCode, string.Join(", ", newState.EnvironmentVariables.Select(e => $"{e.Name} = {e.Value}")), string.Join(", ", newState.Urls.Select(u => $"{u.Name} = {u.Url}")),
                    string.Join(", ", newState.Properties.Select(p => $"{p.Name} = {p.Value}")));
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
